using CoreApiClient;
using CoreApiClient.Attributes;
using CoreApiClient.Extensions;
using CoreApiClient.Models;

namespace CoreApiClient.Examples;

public class DownloadExamples
{
    public static async Task RunAllExamples()
    {
        Console.WriteLine("\n=== Enhanced Download Features Examples ===\n");
        
        await Example1_BasicDownload();
        await Example2_DownloadWithProgress();
        await Example3_DownloadToFile();
        await Example4_DownloadWithResumeSupport();
        await Example5_RangeDownload();
        await Example6_CheckFileInfo();
        await Example7_DownloadWithCancellation();
        
        Console.WriteLine("\n=== All download examples completed ===\n");
    }

    static async Task Example1_BasicDownload()
    {
        Console.WriteLine("--- Example 1: Basic Download ---");
        
        try
        {
            using var httpClient = new HttpClient();
            
            // Download a small file
            var result = await httpClient.DownloadFileAsync(
                "https://jsonplaceholder.typicode.com/posts/1");
            
            Console.WriteLine($"✓ Downloaded {result.SizeFormatted}");
            Console.WriteLine($"✓ Content-Type: {result.ContentType}");
            Console.WriteLine($"✓ Content length: {result.Content?.Length} bytes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    static async Task Example2_DownloadWithProgress()
    {
        Console.WriteLine("--- Example 2: Download with Progress Reporting ---");
        
        try
        {
            using var httpClient = new HttpClient();
            
            var progress = new Progress<DownloadProgress>(p =>
            {
                if (p.IsTotalSizeKnown && p.ProgressPercentage.HasValue)
                {
                    Console.Write($"\rDownloading: {p.ProgressPercentage:F1}% ({p.BytesDownloadedFormatted} / {p.TotalBytesFormatted})");
                }
                else
                {
                    Console.Write($"\rDownloading: {p.BytesDownloadedFormatted}");
                }
            });
            
            var result = await httpClient.DownloadFileAsync(
                "https://jsonplaceholder.typicode.com/photos/1",
                progress);
            
            Console.WriteLine($"\n✓ Download complete: {result.SizeFormatted}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    static async Task Example3_DownloadToFile()
    {
        Console.WriteLine("--- Example 3: Download Directly to File ---");
        
        try
        {
            using var httpClient = new HttpClient();
            var tempFile = Path.Combine(Path.GetTempPath(), $"download_{Guid.NewGuid()}.json");
            
            var progress = new Progress<DownloadProgress>(p =>
            {
                Console.Write($"\rSaving to file: {p.BytesDownloadedFormatted}");
            });
            
            var result = await httpClient.DownloadToFileAsync(
                "https://jsonplaceholder.typicode.com/posts",
                tempFile,
                progress);
            
            Console.WriteLine($"\n✓ File saved: {result.FilePath}");
            Console.WriteLine($"✓ Size: {result.SizeFormatted}");
            Console.WriteLine($"✓ File exists: {File.Exists(tempFile)}");
            
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
                Console.WriteLine("✓ Temporary file cleaned up");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    static async Task Example4_DownloadWithResumeSupport()
    {
        Console.WriteLine("--- Example 4: Download with Resume Support ---");
        
        try
        {
            using var httpClient = new HttpClient();
            var tempFile = Path.Combine(Path.GetTempPath(), $"resume_download_{Guid.NewGuid()}.json");
            
            var progress = new Progress<DownloadProgress>(p =>
            {
                Console.Write($"\rDownloading: {p.BytesDownloadedFormatted}");
            });
            
            // This will automatically resume if interrupted
            var result = await httpClient.DownloadWithResumeAsync(
                "https://jsonplaceholder.typicode.com/posts",
                tempFile,
                progress);
            
            Console.WriteLine($"\n✓ Download completed with resume support");
            Console.WriteLine($"✓ Total downloaded: {result.SizeFormatted}");
            
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    static async Task Example5_RangeDownload()
    {
        Console.WriteLine("--- Example 5: Range/Partial Download ---");
        
        try
        {
            using var httpClient = new HttpClient();
            
            // Check if server supports range requests
            var supportsRanges = await httpClient.SupportsRangeDownloadsAsync(
                "https://jsonplaceholder.typicode.com/posts/1");
            
            Console.WriteLine($"✓ Server supports range downloads: {supportsRanges}");
            
            if (supportsRanges)
            {
                // Download only bytes 0-99
                var result = await httpClient.DownloadRangeAsync(
                    "https://jsonplaceholder.typicode.com/posts/1",
                    0,
                    99);
                
                Console.WriteLine($"✓ Downloaded partial content: {result.BytesDownloaded} bytes");
            }
            else
            {
                Console.WriteLine("✓ Range downloads not supported by this server");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Note: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    static async Task Example6_CheckFileInfo()
    {
        Console.WriteLine("--- Example 6: Check File Info Before Downloading ---");
        
        try
        {
            using var httpClient = new HttpClient();
            
            // Get file size without downloading
            var fileSize = await httpClient.GetFileSizeAsync(
                "https://jsonplaceholder.typicode.com/posts");
            
            if (fileSize.HasValue)
            {
                var sizeMB = fileSize.Value / (1024.0 * 1024.0);
                Console.WriteLine($"✓ File size: {fileSize.Value:N0} bytes ({sizeMB:F2} MB)");
                
                // Decide whether to download based on size
                if (fileSize.Value < 10_000_000) // Less than 10MB
                {
                    Console.WriteLine("✓ File size acceptable, proceeding with download...");
                }
                else
                {
                    Console.WriteLine("⚠ File too large, skipping download");
                }
            }
            else
            {
                Console.WriteLine("✓ File size unknown (will download anyway)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    static async Task Example7_DownloadWithCancellation()
    {
        Console.WriteLine("--- Example 7: Download with Cancellation ---");
        
        try
        {
            using var httpClient = new HttpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            var progress = new Progress<DownloadProgress>(p =>
            {
                Console.Write($"\rDownloading: {p.BytesDownloadedFormatted}");
            });
            
            var result = await httpClient.DownloadFileAsync(
                "https://jsonplaceholder.typicode.com/photos",
                progress,
                cts.Token);
            
            Console.WriteLine($"\n✓ Download completed: {result.SizeFormatted}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n✓ Download was cancelled as expected");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
        
        Console.WriteLine();
    }
}

// Example API interface with download methods
public interface IDownloadApi
{
    // Basic download
    [Get("/files/{id}")]
    Task<byte[]> DownloadFile(string id);
    
    // Download with metadata
    [Get("/files/{id}")]
    Task<ApiResponse<byte[]>> DownloadFileWithHeaders(string id);
    
    // Download with progress (using extension methods with underlying HttpClient)
    // This would be called via the HttpClient extensions shown in examples above
}
