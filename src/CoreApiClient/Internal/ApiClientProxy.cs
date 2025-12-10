using System.Reflection;
using System.Text.Json;
using CoreApiClient.Configuration;
using CoreApiClient.Exceptions;
using CoreApiClient.Models;
using Microsoft.Extensions.Logging;

namespace CoreApiClient.Internal;

/// <summary>
/// Dynamic proxy that intercepts interface method calls and executes HTTP requests.
/// </summary>
/// <typeparam name="T">The API interface type.</typeparam>
internal class ApiClientProxy<T> : DispatchProxy where T : class
{
    private HttpClient? _httpClient;
    private ApiClientOptions? _options;
    private ILogger? _logger;

    /// <summary>
    /// Initializes the proxy with required dependencies.
    /// </summary>
    public void Initialize(HttpClient httpClient, ApiClientOptions options, ILogger? logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    /// <summary>
    /// Intercepts method invocations and executes HTTP requests.
    /// </summary>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null)
        {
            throw new ArgumentNullException(nameof(targetMethod));
        }

        args ??= Array.Empty<object?>();

        // Ensure we're initialized
        if (_httpClient == null || _options == null)
        {
            throw new InvalidOperationException("Proxy has not been initialized");
        }

        // Handle special methods
        if (targetMethod.DeclaringType == typeof(object))
        {
            return HandleObjectMethod(targetMethod, args);
        }

        // Build and execute the HTTP request
        var returnType = targetMethod.ReturnType;

        if (returnType == typeof(Task))
        {
            return InvokeAsync(targetMethod, args);
        }
        else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var method = typeof(ApiClientProxy<T>)
                .GetMethod(nameof(InvokeAsyncGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(resultType);
            return method.Invoke(this, new object[] { targetMethod, args })!;
        }
        else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var method = typeof(ApiClientProxy<T>)
                .GetMethod(nameof(InvokeValueTaskGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(resultType);
            return method.Invoke(this, new object[] { targetMethod, args })!;
        }

        throw new InvalidOperationException(
            $"Method {targetMethod.Name} must return Task, Task<T>, or ValueTask<T>");
    }

    /// <summary>
    /// Handles methods from the Object class (ToString, GetHashCode, Equals).
    /// </summary>
    private object? HandleObjectMethod(MethodInfo method, object?[] args)
    {
        if (method.Name == nameof(ToString) && args.Length == 0)
        {
            return $"ApiClient<{typeof(T).Name}>";
        }
        if (method.Name == nameof(GetHashCode) && args.Length == 0)
        {
            return GetHashCode();
        }
        if (method.Name == nameof(Equals) && args.Length == 1)
        {
            return ReferenceEquals(this, args[0]);
        }

        throw new NotSupportedException($"Method {method.Name} is not supported");
    }

    /// <summary>
    /// Invokes an async method that returns Task (no result).
    /// </summary>
    private async Task InvokeAsync(MethodInfo method, object?[] args)
    {
        var (request, timeout) = RequestBuilder.BuildRequest(
            method, args, _options!.BaseUrl, _options.JsonSerializerOptions, out var cancellationToken);

        await ExecuteRequestAsync(request, method, timeout, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Invokes an async method that returns Task&lt;T&gt;.
    /// </summary>
    private async Task<TResult> InvokeAsyncGeneric<TResult>(MethodInfo method, object?[] args)
    {
        var (request, timeout) = RequestBuilder.BuildRequest(
            method, args, _options!.BaseUrl, _options.JsonSerializerOptions, out var cancellationToken);

        // Check if expecting ApiResponse<T>
        if (RequestBuilder.IsApiResponseType(typeof(TResult)))
        {
            var innerType = RequestBuilder.GetApiResponseInnerType(typeof(TResult));
            var executeMethod = typeof(ApiClientProxy<T>)
                .GetMethod(nameof(ExecuteRequestWithResponseAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(innerType);
            
            var task = (Task)executeMethod.Invoke(this, new object[] { request, method, timeout, cancellationToken })!;
            await task.ConfigureAwait(false);
            
            var resultProperty = task.GetType().GetProperty("Result");
            return (TResult)resultProperty!.GetValue(task)!;
        }

        return await ExecuteRequestAsync<TResult>(request, method, timeout, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Invokes an async method that returns ValueTask&lt;T&gt;.
    /// </summary>
    private async ValueTask<TResult> InvokeValueTaskGeneric<TResult>(MethodInfo method, object?[] args)
    {
        var (request, timeout) = RequestBuilder.BuildRequest(
            method, args, _options!.BaseUrl, _options.JsonSerializerOptions, out var cancellationToken);

        return await ExecuteRequestAsync<TResult>(request, method, timeout, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes an HTTP request without expecting a result.
    /// </summary>
    private async Task ExecuteRequestAsync(
        HttpRequestMessage request, 
        MethodInfo method, 
        TimeSpan? methodTimeout,
        CancellationToken cancellationToken)
    {
        try
        {
            LogRequest(request, method);

            // Create timeout cancellation token if method timeout is specified
            using var timeoutCts = methodTimeout.HasValue 
                ? new CancellationTokenSource(methodTimeout.Value) 
                : null;
            
            var effectiveCt = timeoutCts != null 
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token).Token
                : cancellationToken;

            var response = await _httpClient!.SendAsync(request, HttpCompletionOption.ResponseContentRead, effectiveCt)
                .ConfigureAwait(false);

            await LogResponseAsync(response, method).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode && _options!.ThrowOnError)
            {
                var exception = await ApiException.CreateAsync(response).ConfigureAwait(false);
                throw exception;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.LogWarning("Request for method {MethodName} was cancelled", method.Name);
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Request for method {MethodName} timed out", method.Name);
            throw new TimeoutException($"Request for method {method.Name} timed out");
        }
        catch (Exception ex) when (ex is not ApiException)
        {
            _logger?.LogError(ex, "Error executing request for method {MethodName}", method.Name);
            throw;
        }
        finally
        {
            request.Dispose();
        }
    }

    /// <summary>
    /// Executes an HTTP request and deserializes the response.
    /// </summary>
    private async Task<TResult> ExecuteRequestAsync<TResult>(
        HttpRequestMessage request, 
        MethodInfo method,
        TimeSpan? methodTimeout,
        CancellationToken cancellationToken)
    {
        try
        {
            LogRequest(request, method);

            // Create timeout cancellation token if method timeout is specified
            using var timeoutCts = methodTimeout.HasValue 
                ? new CancellationTokenSource(methodTimeout.Value) 
                : null;
            
            var effectiveCt = timeoutCts != null 
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token).Token
                : cancellationToken;

            var response = await _httpClient!.SendAsync(request, HttpCompletionOption.ResponseContentRead, effectiveCt)
                .ConfigureAwait(false);

            await LogResponseAsync(response, method).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode && _options!.ThrowOnError)
            {
                var exception = await ApiException.CreateAsync(response).ConfigureAwait(false);
                throw exception;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(content))
            {
                return default!;
            }

            // Handle string return type
            if (typeof(TResult) == typeof(string))
            {
                return (TResult)(object)content;
            }

            // Handle HttpResponseMessage return type
            if (typeof(TResult) == typeof(HttpResponseMessage))
            {
                return (TResult)(object)response;
            }

            // Deserialize JSON
            var result = JsonSerializer.Deserialize<TResult>(content, _options!.JsonSerializerOptions);
            return result!;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.LogWarning("Request for method {MethodName} was cancelled", method.Name);
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Request for method {MethodName} timed out", method.Name);
            throw new TimeoutException($"Request for method {method.Name} timed out");
        }
        catch (Exception ex) when (ex is not ApiException)
        {
            _logger?.LogError(ex, "Error executing request for method {MethodName}", method.Name);
            throw;
        }
        finally
        {
            request.Dispose();
        }
    }

    /// <summary>
    /// Executes an HTTP request and returns full response with headers.
    /// </summary>
    private async Task<ApiResponse<TContent>> ExecuteRequestWithResponseAsync<TContent>(
        HttpRequestMessage request,
        MethodInfo method,
        TimeSpan? methodTimeout,
        CancellationToken cancellationToken)
    {
        try
        {
            LogRequest(request, method);

            using var timeoutCts = methodTimeout.HasValue 
                ? new CancellationTokenSource(methodTimeout.Value) 
                : null;
            
            var effectiveCt = timeoutCts != null 
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token).Token
                : cancellationToken;

            var response = await _httpClient!.SendAsync(request, HttpCompletionOption.ResponseContentRead, effectiveCt)
                .ConfigureAwait(false);

            await LogResponseAsync(response, method).ConfigureAwait(false);

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            TContent? deserializedContent = default;
            if (!string.IsNullOrWhiteSpace(content))
            {
                if (typeof(TContent) == typeof(string))
                {
                    deserializedContent = (TContent)(object)content;
                }
                else
                {
                    deserializedContent = JsonSerializer.Deserialize<TContent>(content, _options!.JsonSerializerOptions);
                }
            }

            var headers = response.Headers
                .Concat(response.Content.Headers)
                .ToDictionary(h => h.Key, h => h.Value);

            return new ApiResponse<TContent>
            {
                Content = deserializedContent,
                StatusCode = response.StatusCode,
                Headers = headers,
                ReasonPhrase = response.ReasonPhrase
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.LogWarning("Request for method {MethodName} was cancelled", method.Name);
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Request for method {MethodName} timed out", method.Name);
            throw new TimeoutException($"Request for method {method.Name} timed out");
        }
        finally
        {
            request.Dispose();
        }
    }

    /// <summary>
    /// Logs the HTTP request.
    /// </summary>
    private void LogRequest(HttpRequestMessage request, MethodInfo method)
    {
        if (!_options!.EnableLogging || _logger == null)
            return;

        _logger.LogInformation(
            "Executing {MethodName}: {HttpMethod} {RequestUri}",
            method.Name,
            request.Method,
            request.RequestUri);

        if (_options.LogRequestBody && request.Content != null)
        {
            var body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            _logger.LogDebug("Request Body: {RequestBody}", body);
        }
    }

    /// <summary>
    /// Logs the HTTP response.
    /// </summary>
    private async Task LogResponseAsync(HttpResponseMessage response, MethodInfo method)
    {
        if (!_options!.EnableLogging || _logger == null)
            return;

        _logger.LogInformation(
            "Response for {MethodName}: {StatusCode} {ReasonPhrase}",
            method.Name,
            (int)response.StatusCode,
            response.ReasonPhrase);

        if (_options.LogResponseBody)
        {
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.LogDebug("Response Body: {ResponseBody}", body);
        }
    }
}
