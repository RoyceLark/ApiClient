using System.Reflection;
using CoreApiClient.Configuration;
using CoreApiClient.Exceptions;
using CoreApiClient.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
namespace CoreApiClient;
public static class ApiClient
{
    public static T Create<T>(HttpClient httpClient, ApiClientOptions options, ILogger? logger = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        var type = typeof(T);
        if (!type.IsInterface) throw new ArgumentException($"Type {type.Name} must be an interface");
        try { options.Validate(); } catch (Exception ex) { throw new ApiConfigurationException("Invalid API client configuration", ex); }
        ConfigureHttpClient(httpClient, options);
        var proxy = DispatchProxy.Create<T, ApiClientProxy<T>>() as ApiClientProxy<T>;
        if (proxy == null) throw new InvalidOperationException("Failed to create dispatch proxy");
        proxy.Initialize(httpClient, options, logger ?? NullLogger.Instance);
        return (proxy as T)!;
    }
    public static T Create<T>(string baseUrl, ILogger? logger = null) where T : class
    {
        return Create<T>(new HttpClient(), new ApiClientOptions { BaseUrl = baseUrl }, logger);
    }
    public static T Create<T>(string baseUrl, Action<ApiClientOptions> configure, ILogger? logger = null) where T : class
    {
        var options = new ApiClientOptions { BaseUrl = baseUrl };
        configure?.Invoke(options);
        return Create<T>(new HttpClient(), options, logger);
    }
    private static void ConfigureHttpClient(HttpClient httpClient, ApiClientOptions options)
    {
        httpClient.Timeout = options.Timeout;
        httpClient.MaxResponseContentBufferSize = options.MaxResponseContentBufferSize;
        foreach (var header in options.DefaultHeaders)
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
    }
    public static void ValidateInterface<T>() where T : class
    {
        var type = typeof(T);
        if (!type.IsInterface) throw new ApiConfigurationException($"Type {type.Name} must be an interface");
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            var returnType = method.ReturnType;
            var isValid = returnType == typeof(Task) ||
                         (returnType.IsGenericType && (returnType.GetGenericTypeDefinition() == typeof(Task<>) ||
                                                      returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)));
            if (!isValid) throw new ApiConfigurationException($"Method {method.Name} must return Task, Task<T>, or ValueTask<T>");
            if (!method.GetCustomAttributes<Attributes.HttpMethodAttribute>().Any())
                throw new ApiConfigurationException($"Method {method.Name} must have an HTTP method attribute");
        }
    }
}
