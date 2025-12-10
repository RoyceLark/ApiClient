using System.Text.Json;
namespace CoreApiClient.Configuration;
public class ApiClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public IDictionary<string, string> DefaultHeaders { get; set; } = new Dictionary<string, string>();
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public bool EnableLogging { get; set; } = true;
    public bool LogRequestBody { get; set; } = true;
    public bool LogResponseBody { get; set; } = true;
    public RetryPolicyOptions RetryPolicy { get; set; } = new();
    public bool ThrowOnError { get; set; } = true;
    public long MaxResponseContentBufferSize { get; set; } = 10 * 1024 * 1024;
    public bool EnableTelemetry { get; set; } = false;
    public bool EnableRecording { get; set; } = false;
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl)) throw new ArgumentException("BaseUrl must be configured");
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _)) throw new ArgumentException("BaseUrl must be a valid absolute URI");
        if (Timeout <= TimeSpan.Zero) throw new ArgumentException("Timeout must be greater than zero");
        RetryPolicy.Validate();
    }
}
public class RetryPolicyOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public bool UseExponentialBackoff { get; set; } = true;
    public double BackoffMultiplier { get; set; } = 2.0;
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    public ISet<int> RetryableStatusCodes { get; set; } = new HashSet<int> { 408, 429, 500, 502, 503, 504 };
    public void Validate()
    {
        if (MaxRetryAttempts < 0) throw new ArgumentException("MaxRetryAttempts must be non-negative");
        if (RetryDelay < TimeSpan.Zero) throw new ArgumentException("RetryDelay must be non-negative");
        if (BackoffMultiplier <= 0) throw new ArgumentException("BackoffMultiplier must be greater than zero");
    }
}
