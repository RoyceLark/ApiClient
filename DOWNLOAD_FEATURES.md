# CoreApiClient v1.2 - Enhanced Download Features

## 🚀 New in v1.2

Complete file download support with progress tracking, streaming, and resume capabilities!

## ✨ Features Added

### 1. **Progress Reporting** ✅
Track download progress in real-time:

```csharp
var progress = new Progress<DownloadProgress>(p =>
{
    Console.WriteLine($"Downloaded: {p.ProgressPercentage:F1}% ({p.BytesDownloadedFormatted})");
});

var result = await httpClient.DownloadFileAsync(url, progress);
```

### 2. **Stream to File** ✅
Memory-efficient downloads directly to disk:

```csharp
var result = await httpClient.DownloadToFileAsync(
    "https://example.com/largefile.zip",
    "/path/to/save/file.zip",
    progress);

Console.WriteLine($"Saved to: {result.FilePath}");
Console.WriteLine($"Size: {result.SizeFormatted}");
```

### 3. **Range/Partial Downloads** ✅
Download specific byte ranges:

```csharp
// Download bytes 1000-2000
var result = await httpClient.DownloadRangeAsync(url, 1000, 2000);

// Download from byte 5000 to end
var result = await httpClient.DownloadRangeAsync(url, 5000);
```

### 4. **Resume Support** ✅
Automatically resume interrupted downloads:

```csharp
var result = await httpClient.DownloadWithResumeAsync(
    url,
    filePath,
    progress,
    cancellationToken,
    maxRetries: 3);
```

### 5. **File Info Before Download** ✅
Check file size and range support:

```csharp
// Get file size without downloading
var size = await httpClient.GetFileSizeAsync(url);
Console.WriteLine($"File size: {size} bytes");

// Check if resumable
var supportsRanges = await httpClient.SupportsRangeDownloadsAsync(url);
Console.WriteLine($"Supports resume: {supportsRanges}");
```

### 6. **Cancellation Support** ✅
Cancel downloads anytime:

```csharp
using var cts = new CancellationTokenSource();

var task = httpClient.DownloadFileAsync(url, progress, cts.Token);

// Cancel after 5 seconds
cts.CancelAfter(TimeSpan.FromSeconds(5));

try
{
    await task;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Download cancelled");
}
```

## 📊 Download Progress Information

The `DownloadProgress` class provides:

```csharp
public class DownloadProgress
{
    public long? TotalBytes { get; set; }           // Total file size (if known)
    public long BytesDownloaded { get; set; }       // Bytes downloaded so far
    public double? ProgressPercentage { get; }      // 0-100%
    public bool IsTotalSizeKnown { get; }          // Whether size is known
    public string BytesDownloadedFormatted { get; } // e.g., "1.5 MB"
    public string? TotalBytesFormatted { get; }    // e.g., "10 MB"
}
```

## 📦 Download Result

The `DownloadResult` class provides:

```csharp
public class DownloadResult
{
    public byte[]? Content { get; }                 // Downloaded bytes (if in memory)
    public string? FilePath { get; }                // Path where saved (if to file)
    public long BytesDownloaded { get; }            // Total bytes downloaded
    public string? ContentType { get; }             // MIME type
    public string? SuggestedFileName { get; }       // From Content-Disposition
    public IReadOnlyDictionary Headers { get; }     // All response headers
    public bool IsSavedToFile { get; }             // Whether saved to file
    public string SizeFormatted { get; }           // Human-readable size
}
```

## 🎯 Complete Examples

### Example 1: Download with Progress Bar

```csharp
var progress = new Progress<DownloadProgress>(p =>
{
    if (p.IsTotalSizeKnown)
    {
        var percent = p.ProgressPercentage!.Value;
        var bar = new string('█', (int)(percent / 2));
        Console.Write($"\r[{bar,-50}] {percent:F1}% ({p.BytesDownloadedFormatted}/{p.TotalBytesFormatted})");
    }
});

var result = await httpClient.DownloadToFileAsync(url, filePath, progress);
Console.WriteLine($"\nDownload complete: {result.SizeFormatted}");
```

### Example 2: Resume Interrupted Download

```csharp
string filePath = "large-video.mp4";
string url = "https://example.com/video.mp4";

try
{
    var result = await httpClient.DownloadWithResumeAsync(
        url,
        filePath,
        progress: new Progress<DownloadProgress>(p => 
            Console.WriteLine($"Progress: {p.BytesDownloadedFormatted}")),
        maxRetries: 5);
        
    Console.WriteLine($"Download complete: {result.FilePath}");
}
catch (Exception ex)
{
    Console.WriteLine($"Download failed: {ex.Message}");
    // File is partially downloaded and can be resumed later
}
```

### Example 3: Smart Download Decision

```csharp
var url = "https://example.com/largefile.zip";

// Check file size first
var fileSize = await httpClient.GetFileSizeAsync(url);

if (fileSize.HasValue)
{
    var sizeMB = fileSize.Value / (1024.0 * 1024.0);
    Console.WriteLine($"File size: {sizeMB:F2} MB");
    
    if (sizeMB > 100)
    {
        Console.WriteLine("Large file detected, using resume-capable download...");
        await httpClient.DownloadWithResumeAsync(url, "file.zip", progress);
    }
    else
    {
        Console.WriteLine("Small file, using direct download...");
        await httpClient.DownloadToFileAsync(url, "file.zip", progress);
    }
}
```

### Example 4: Download Chunk by Chunk

```csharp
var url = "https://example.com/file.zip";
var totalSize = await httpClient.GetFileSizeAsync(url);

if (totalSize.HasValue)
{
    const long chunkSize = 1024 * 1024; // 1MB chunks
    long offset = 0;
    
    using var fileStream = File.Create("file.zip");
    
    while (offset < totalSize.Value)
    {
        var end = Math.Min(offset + chunkSize - 1, totalSize.Value - 1);
        
        var chunk = await httpClient.DownloadRangeAsync(url, offset, end);
        await fileStream.WriteAsync(chunk.Content, 0, chunk.Content!.Length);
        
        offset += chunk.BytesDownloaded;
        Console.WriteLine($"Downloaded: {offset}/{totalSize.Value} bytes");
    }
}
```

## 🔄 Upload vs Download Comparison

| Feature | Upload | Download |
|---------|--------|----------|
| Single file | ✅ | ✅ |
| Multiple files | ✅ | ✅ (sequential) |
| Progress tracking | ❌ | ✅ **NEW** |
| From/To file | ✅ | ✅ **NEW** |
| From/To bytes | ✅ | ✅ |
| From/To stream | ✅ | ✅ |
| Resume support | ❌ | ✅ **NEW** |
| Range requests | ❌ | ✅ **NEW** |
| Cancellation | ✅ | ✅ |
| Content-Type | ✅ | ✅ |
| Metadata | ✅ | ✅ **NEW** |

## 🎉 Benefits

### For Users:
- ✅ **Visual feedback** with progress reporting
- ✅ **Resume capability** for large files
- ✅ **Memory efficient** with streaming
- ✅ **Cancel anytime** with CancellationToken
- ✅ **Smart decisions** with pre-download info

### For Developers:
- ✅ **Simple API** - Just extension methods
- ✅ **Flexible** - Memory or file, your choice
- ✅ **Robust** - Automatic retry and resume
- ✅ **Testable** - Easy to mock
- ✅ **Production-ready** - Comprehensive error handling

## 🚀 Upgrade from v1.1

100% backwards compatible! All v1.1 code continues to work.

New download features are opt-in via extension methods.

```bash
# Update package
dotnet add package CoreApiClient --version 1.2.0
```

## 📝 Summary

**v1.2 completes the file transfer story:**
- v1.0: Core API client
- v1.1: File uploads + enterprise features
- v1.2: **Enhanced downloads with progress & resume** ✨

**Now truly feature-complete for production use!** 🎉
