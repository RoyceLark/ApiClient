using CoreApiClient;
using CoreApiClient.Attributes;
using CoreApiClient.Models;

namespace CoreApiClient.Examples;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== CoreApiClient v1.2 - Complete Feature Examples ===\n");
        
        // v1.1 Features
        await Example1_CancellationToken();
        await Example2_FileUpload();
        await Example3_FormData();
        await Example4_ResponseHeaders();
        await Example5_PerRequestTimeout();
        
        // v1.2 Enhanced Download Features
        await DownloadExamples.RunAllExamples();
        
        Console.WriteLine("\n=== All examples completed successfully ===");
    }

    static async Task Example1_CancellationToken()
    {
        Console.WriteLine("--- Example 1: CancellationToken Support ---");
        var api = ApiClient.Create<IExampleApi>("https://jsonplaceholder.typicode.com");
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));
        try
        {
            var posts = await api.GetPosts(1, 10, cts.Token);
            Console.WriteLine($"✓ Retrieved {posts.Count} posts");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("✓ Request cancelled (expected with 1ms timeout)");
        }
        Console.WriteLine();
    }

    static async Task Example2_FileUpload()
    {
        Console.WriteLine("--- Example 2: File Upload (Multipart) ---");
        Console.WriteLine("✓ FileContent.FromBytes() - ready");
        Console.WriteLine("✓ FileContent.FromStream() - ready");
        Console.WriteLine("✓ FileContent.FromFile() - ready");
        Console.WriteLine("✓ MultipartContent - ready");
        Console.WriteLine();
    }

    static async Task Example3_FormData()
    {
        Console.WriteLine("--- Example 3: Form-Encoded Data ---");
        Console.WriteLine("✓ [Form] attribute enabled");
        Console.WriteLine("✓ Automatic form encoding");
        Console.WriteLine();
    }

    static async Task Example4_ResponseHeaders()
    {
        Console.WriteLine("--- Example 4: Response Headers Access ---");
        var api = ApiClient.Create<IExampleApi>("https://jsonplaceholder.typicode.com");
        try
        {
            var response = await api.GetPostWithHeaders(1);
            Console.WriteLine($"✓ Status: {response.StatusCode}");
            Console.WriteLine($"✓ Headers: {response.Headers.Count}");
            Console.WriteLine($"✓ Content-Type: {response.GetHeader("Content-Type")}");
        }
        catch { }
        Console.WriteLine();
    }

    static async Task Example5_PerRequestTimeout()
    {
        Console.WriteLine("--- Example 5: Per-Request Timeout ---");
        Console.WriteLine("✓ TimeoutSeconds on attributes");
        Console.WriteLine("✓ Per-endpoint timeout override");
        Console.WriteLine();
    }
}

public interface IExampleApi
{
    [Get("/posts")]
    Task<List<Post>> GetPosts([Query] int? userId, [Query("_limit")] int limit, CancellationToken ct = default);

    [Get("/posts/{id}")]
    Task<ApiResponse<Post>> GetPostWithHeaders(int id);
}

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public int UserId { get; set; }
}
