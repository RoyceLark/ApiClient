using CoreApiClient.Models;

namespace CoreApiClient.Extensions;

/// <summary>
/// Extension methods for enhanced download operations.
/// </summary>
public static class DownloadExtensions
{
    /// <summary>
    /// Downloads a file with progress reporting.
    /// </summary>
    /// <param name="httpClient">The HttpClient instance.</param>
    /// <param name="url">The URL to download from.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The download result.</returns>
    public static async Task<DownloadResult> DownloadFileAsync(
        this HttpClient httpClient,
        string url,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var options = new DownloadOptions
        {
            Progress = progress,
            CancellationToken = cancellationToken
        };

        return await Internal.DownloadHelper.DownloadAsync(httpClient, url, options).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads a file directly to disk with progress reporting.
    /// </summary>
    /// <param name="httpClient">The HttpClient instance.</param>
    /// <param name="url">The URL to download from.</param>
    /// <param name="filePath">The path to save the file to.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The download result.</returns>
    public static async Task<DownloadResult> DownloadToFileAsync(
        this HttpClient httpClient,
        string url,
        string filePath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var options = new DownloadOptions
        {
            SaveToPath = filePath,
            Progress = progress,
            CancellationToken = cancellationToken
        };

        return await Internal.DownloadHelper.DownloadAsync(httpClient, url, options).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads a range of bytes from a file (for resume/partial downloads).
    /// </summary>
    /// <param name="httpClient">The HttpClient instance.</param>
    /// <param name="url">The URL to download from.</param>
    /// <param name="start">The start byte position.</param>
    /// <param name="end">The end byte position (optional).</param>
    /// <param name="progress">Progress reporter.</param>
    /// <returns>The download result.</returns>
    public static async Task<DownloadResult> DownloadRangeAsync(
        this HttpClient httpClient,
        string url,
        long start,
        long? end = null,
        IProgress<DownloadProgress>? progress = null)
    {
        var options = new RangeDownloadOptions
        {
            Start = start,
            End = end,
            Progress = progress
        };

        return await Internal.DownloadHelper.DownloadRangeAsync(httpClient, url, options).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if the server supports range/resume downloads.
    /// </summary>
    /// <param name="httpClient">The HttpClient instance.</param>
    /// <param name="url">The URL to check.</param>
    /// <returns>True if range requests are supported.</returns>
    public static async Task<bool> SupportsRangeDownloadsAsync(
        this HttpClient httpClient,
        string url)
    {
        return await Internal.DownloadHelper.SupportsRangeRequestsAsync(httpClient, url).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the file size without downloading it.
    /// </summary>
    /// <param name="httpClient">The HttpClient instance.</param>
    /// <param name="url">The URL to check.</param>
    /// <returns>The file size in bytes, or null if unknown.</returns>
    public static async Task<long?> GetFileSizeAsync(
        this HttpClient httpClient,
        string url)
    {
        return await Internal.DownloadHelper.GetContentLengthAsync(httpClient, url).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads a file with automatic resume if the download is interrupted.
    /// </summary>
    /// <param name="httpClient">The HttpClient instance.</param>
    /// <param name="url">The URL to download from.</param>
    /// <param name="filePath">The path to save the file to.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <returns>The download result.</returns>
    public static async Task<DownloadResult> DownloadWithResumeAsync(
        this HttpClient httpClient,
        string url,
        string filePath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default,
        int maxRetries = 3)
    {
        // Check if server supports ranges
        var supportsRanges = await httpClient.SupportsRangeDownloadsAsync(url).ConfigureAwait(false);
        
        if (!supportsRanges)
        {
            // Fall back to regular download if ranges not supported
            return await httpClient.DownloadToFileAsync(url, filePath, progress, cancellationToken).ConfigureAwait(false);
        }

        int retryCount = 0;
        long bytesDownloaded = 0;

        // Check if partial file exists
        if (File.Exists(filePath))
        {
            bytesDownloaded = new FileInfo(filePath).Length;
        }

        while (retryCount < maxRetries)
        {
            try
            {
                if (bytesDownloaded > 0)
                {
                    // Resume from where we left off
                    var rangeResult = await httpClient.DownloadRangeAsync(url, bytesDownloaded, null, progress).ConfigureAwait(false);
                    
                    // Append to existing file
                    if (rangeResult.Content != null)
                    {
                        await File.WriteAllBytesAsync(filePath, rangeResult.Content, cancellationToken).ConfigureAwait(false);
                    }

                    return new DownloadResult
                    {
                        FilePath = filePath,
                        BytesDownloaded = bytesDownloaded + (rangeResult.Content?.Length ?? 0),
                        ContentType = rangeResult.ContentType,
                        SuggestedFileName = rangeResult.SuggestedFileName,
                        Headers = rangeResult.Headers
                    };
                }
                else
                {
                    // Start fresh download
                    return await httpClient.DownloadToFileAsync(url, filePath, progress, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    throw;
                }

                // Update bytes downloaded for next retry
                if (File.Exists(filePath))
                {
                    bytesDownloaded = new FileInfo(filePath).Length;
                }

                // Wait before retrying (exponential backoff)
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("Download failed after maximum retries");
    }
}

/// <summary>
/// Extension methods specifically for File class operations.
/// </summary>
public static class FileExtensions
{
    /// <summary>
    /// Appends bytes to a file asynchronously.
    /// </summary>
    public static async Task AppendAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
    {
        using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None, 4096, true);
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }
}
