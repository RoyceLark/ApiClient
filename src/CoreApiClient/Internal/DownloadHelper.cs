using System.Net.Http.Headers;
using CoreApiClient.Models;

namespace CoreApiClient.Internal;

/// <summary>
/// Helper class for enhanced download operations with progress tracking and streaming.
/// </summary>
internal static class DownloadHelper
{
    /// <summary>
    /// Downloads content with progress reporting and optional file streaming.
    /// </summary>
    public static async Task<DownloadResult> DownloadAsync(
        HttpClient httpClient,
        string url,
        DownloadOptions? options = null,
        Dictionary<string, string>? headers = null)
    {
        options ??= new DownloadOptions();
        
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        // Add custom headers
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        using var response = await httpClient.SendAsync(
            request, 
            HttpCompletionOption.ResponseHeadersRead, 
            options.CancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        var contentType = response.Content.Headers.ContentType?.MediaType;
        var suggestedFileName = GetFileNameFromHeaders(response.Content.Headers);

        var responseHeaders = response.Headers
            .Concat(response.Content.Headers)
            .ToDictionary(h => h.Key, h => h.Value);

        // If SaveToPath is specified, stream directly to file
        if (!string.IsNullOrEmpty(options.SaveToPath))
        {
            var bytesDownloaded = await DownloadToFileAsync(
                response, 
                options.SaveToPath, 
                totalBytes, 
                options.BufferSize, 
                options.Progress,
                options.OverwriteExisting,
                options.CancellationToken).ConfigureAwait(false);

            return new DownloadResult
            {
                FilePath = options.SaveToPath,
                BytesDownloaded = bytesDownloaded,
                ContentType = contentType,
                SuggestedFileName = suggestedFileName,
                Headers = responseHeaders
            };
        }
        else
        {
            // Download to memory with progress
            var content = await DownloadToMemoryAsync(
                response, 
                totalBytes, 
                options.BufferSize, 
                options.Progress,
                options.CancellationToken).ConfigureAwait(false);

            return new DownloadResult
            {
                Content = content,
                BytesDownloaded = content.Length,
                ContentType = contentType,
                SuggestedFileName = suggestedFileName,
                Headers = responseHeaders
            };
        }
    }

    /// <summary>
    /// Downloads content with range support (for resume/partial downloads).
    /// </summary>
    public static async Task<DownloadResult> DownloadRangeAsync(
        HttpClient httpClient,
        string url,
        RangeDownloadOptions options,
        Dictionary<string, string>? additionalHeaders = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        // Add Range header
        request.Headers.Range = new RangeHeaderValue(options.Start, options.End);
        
        // Add custom headers
        if (additionalHeaders != null)
        {
            foreach (var header in additionalHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        using var response = await httpClient.SendAsync(
            request, 
            HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        var contentType = response.Content.Headers.ContentType?.MediaType;
        var suggestedFileName = GetFileNameFromHeaders(response.Content.Headers);

        var responseHeaders = response.Headers
            .Concat(response.Content.Headers)
            .ToDictionary(h => h.Key, h => h.Value);

        var content = await DownloadToMemoryAsync(
            response, 
            totalBytes, 
            81920, 
            options.Progress,
            CancellationToken.None).ConfigureAwait(false);

        return new DownloadResult
        {
            Content = content,
            BytesDownloaded = content.Length,
            ContentType = contentType,
            SuggestedFileName = suggestedFileName,
            Headers = responseHeaders
        };
    }

    /// <summary>
    /// Downloads content directly to a file with progress reporting.
    /// </summary>
    private static async Task<long> DownloadToFileAsync(
        HttpResponseMessage response,
        string filePath,
        long? totalBytes,
        int bufferSize,
        IProgress<DownloadProgress>? progress,
        bool overwriteExisting,
        CancellationToken cancellationToken)
    {
        // Check if file exists
        if (File.Exists(filePath) && !overwriteExisting)
        {
            throw new IOException($"File already exists: {filePath}");
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        long bytesDownloaded = 0;
        var buffer = new byte[bufferSize];

        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true);

        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            bytesDownloaded += bytesRead;

            // Report progress
            progress?.Report(new DownloadProgress
            {
                TotalBytes = totalBytes,
                BytesDownloaded = bytesDownloaded
            });
        }

        return bytesDownloaded;
    }

    /// <summary>
    /// Downloads content to memory with progress reporting.
    /// </summary>
    private static async Task<byte[]> DownloadToMemoryAsync(
        HttpResponseMessage response,
        long? totalBytes,
        int bufferSize,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var memoryStream = new MemoryStream();

        long bytesDownloaded = 0;
        var buffer = new byte[bufferSize];
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await memoryStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            bytesDownloaded += bytesRead;

            // Report progress
            progress?.Report(new DownloadProgress
            {
                TotalBytes = totalBytes,
                BytesDownloaded = bytesDownloaded
            });
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Extracts filename from Content-Disposition header.
    /// </summary>
    private static string? GetFileNameFromHeaders(HttpContentHeaders headers)
    {
        if (headers.ContentDisposition?.FileName != null)
        {
            return headers.ContentDisposition.FileName.Trim('"');
        }

        if (headers.ContentDisposition?.FileNameStar != null)
        {
            return headers.ContentDisposition.FileNameStar;
        }

        return null;
    }

    /// <summary>
    /// Checks if the server supports range requests.
    /// </summary>
    public static async Task<bool> SupportsRangeRequestsAsync(HttpClient httpClient, string url)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            
            return response.Headers.AcceptRanges?.Contains("bytes") == true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the total file size without downloading.
    /// </summary>
    public static async Task<long?> GetContentLengthAsync(HttpClient httpClient, string url)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            
            return response.Content.Headers.ContentLength;
        }
        catch
        {
            return null;
        }
    }
}
