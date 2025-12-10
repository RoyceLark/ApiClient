namespace CoreApiClient.Models;

/// <summary>
/// Options for file downloads with progress tracking and streaming.
/// </summary>
public class DownloadOptions
{
    /// <summary>
    /// Gets or sets the file path to save the downloaded content to.
    /// If specified, content will be streamed directly to this file.
    /// </summary>
    public string? SaveToPath { get; set; }

    /// <summary>
    /// Gets or sets the progress reporter for download progress.
    /// </summary>
    public IProgress<DownloadProgress>? Progress { get; set; }

    /// <summary>
    /// Gets or sets the buffer size for streaming downloads (default: 81920 bytes = 80KB).
    /// </summary>
    public int BufferSize { get; set; } = 81920;

    /// <summary>
    /// Gets or sets whether to overwrite existing files (default: true).
    /// </summary>
    public bool OverwriteExisting { get; set; } = true;

    /// <summary>
    /// Gets or sets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = default;
}

/// <summary>
/// Represents download progress information.
/// </summary>
public class DownloadProgress
{
    /// <summary>
    /// Gets or sets the total number of bytes to download (if known).
    /// </summary>
    public long? TotalBytes { get; set; }

    /// <summary>
    /// Gets or sets the number of bytes downloaded so far.
    /// </summary>
    public long BytesDownloaded { get; set; }

    /// <summary>
    /// Gets the download progress as a percentage (0-100).
    /// </summary>
    public double? ProgressPercentage => TotalBytes.HasValue && TotalBytes.Value > 0
        ? (BytesDownloaded / (double)TotalBytes.Value) * 100
        : null;

    /// <summary>
    /// Gets whether the total size is known.
    /// </summary>
    public bool IsTotalSizeKnown => TotalBytes.HasValue && TotalBytes.Value > 0;

    /// <summary>
    /// Gets a human-readable representation of bytes downloaded.
    /// </summary>
    public string BytesDownloadedFormatted => FormatBytes(BytesDownloaded);

    /// <summary>
    /// Gets a human-readable representation of total bytes.
    /// </summary>
    public string? TotalBytesFormatted => TotalBytes.HasValue ? FormatBytes(TotalBytes.Value) : null;

    /// <summary>
    /// Formats bytes into a human-readable string.
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Options for range/partial downloads.
/// </summary>
public class RangeDownloadOptions
{
    /// <summary>
    /// Gets or sets the start byte position (inclusive).
    /// </summary>
    public long Start { get; set; }

    /// <summary>
    /// Gets or sets the end byte position (inclusive). If null, downloads to the end.
    /// </summary>
    public long? End { get; set; }

    /// <summary>
    /// Gets or sets the progress reporter.
    /// </summary>
    public IProgress<DownloadProgress>? Progress { get; set; }

    /// <summary>
    /// Gets the Range header value.
    /// </summary>
    public string GetRangeHeader() => End.HasValue 
        ? $"bytes={Start}-{End.Value}" 
        : $"bytes={Start}-";
}

/// <summary>
/// Result of a download operation.
/// </summary>
public class DownloadResult
{
    /// <summary>
    /// Gets or sets the downloaded content as bytes (if not streamed to file).
    /// </summary>
    public byte[]? Content { get; init; }

    /// <summary>
    /// Gets or sets the file path where content was saved (if streamed to file).
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets or sets the total number of bytes downloaded.
    /// </summary>
    public long BytesDownloaded { get; init; }

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets or sets the suggested filename from Content-Disposition header.
    /// </summary>
    public string? SuggestedFileName { get; init; }

    /// <summary>
    /// Gets or sets all response headers.
    /// </summary>
    public IReadOnlyDictionary<string, IEnumerable<string>>? Headers { get; init; }

    /// <summary>
    /// Gets whether the content was saved to a file.
    /// </summary>
    public bool IsSavedToFile => !string.IsNullOrEmpty(FilePath);

    /// <summary>
    /// Gets a human-readable size string.
    /// </summary>
    public string SizeFormatted
    {
        get
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = BytesDownloaded;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}

/// <summary>
/// Attribute to indicate a method performs a download with enhanced features.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class DownloadAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether to support range/resume downloads.
    /// </summary>
    public bool SupportRanges { get; set; } = false;

    /// <summary>
    /// Gets or sets the default buffer size for streaming.
    /// </summary>
    public int BufferSize { get; set; } = 81920; // 80KB
}
