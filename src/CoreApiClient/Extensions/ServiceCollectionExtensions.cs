using CoreApiClient.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
namespace CoreApiClient.Extensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiClient<T>(this IServiceCollection services, Action<ApiClientOptions> configure) where T : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        var httpClientBuilder = services.AddHttpClient<T>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ApiClientOptions>>().Value;
            client.Timeout = options.Timeout;
            client.MaxResponseContentBufferSize = options.MaxResponseContentBufferSize;
            foreach (var header in options.DefaultHeaders)
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        });
        httpClientBuilder.AddPolicyHandler((sp, request) =>
        {
            var options = sp.GetRequiredService<IOptions<ApiClientOptions>>().Value;
            return CreateRetryPolicy(options.RetryPolicy);
        });
        httpClientBuilder.AddPolicyHandler((sp, request) =>
        {
            var options = sp.GetRequiredService<IOptions<ApiClientOptions>>().Value;
            return Policy.TimeoutAsync<HttpResponseMessage>(options.Timeout);
        });
        services.AddTransient<T>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(typeof(T).Name);
            var options = sp.GetRequiredService<IOptions<ApiClientOptions>>().Value;
            var logger = sp.GetService<ILogger<T>>();
            return ApiClient.Create<T>(httpClient, options, logger);
        });
        return services;
    }
    public static IServiceCollection AddApiClient<T>(this IServiceCollection services, string baseUrl) where T : class
    {
        return services.AddApiClient<T>(options => { options.BaseUrl = baseUrl; });
    }
    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(RetryPolicyOptions options)
    {
        if (!options.Enabled) return Policy.NoOpAsync<HttpResponseMessage>();
        var retryPolicy = HttpPolicyExtensions.HandleTransientHttpError()
            .OrResult(response => options.RetryableStatusCodes.Contains((int)response.StatusCode));
        if (options.UseExponentialBackoff)
        {
            return retryPolicy.WaitAndRetryAsync(options.MaxRetryAttempts, retryAttempt =>
            {
                var delay = TimeSpan.FromMilliseconds(options.RetryDelay.TotalMilliseconds * Math.Pow(options.BackoffMultiplier, retryAttempt - 1));
                return delay > options.MaxRetryDelay ? options.MaxRetryDelay : delay;
            });
        }
        return retryPolicy.WaitAndRetryAsync(options.MaxRetryAttempts, retryAttempt => options.RetryDelay);
    }
}
