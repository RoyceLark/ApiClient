using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CoreApiClient.Attributes;
using CoreApiClient.Models;
using MultipartContent = CoreApiClient.Models.MultipartContent;

namespace CoreApiClient.Internal;

/// <summary>
/// Builds HTTP requests from method invocations.
/// </summary>
public class RequestBuilder
{
    private static readonly Regex RouteParameterRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);

    /// <summary>
    /// Builds an HttpRequestMessage from a method invocation.
    /// </summary>
    /// <param name="method">The method info.</param>
    /// <param name="args">The method arguments.</param>
    /// <param name="baseUrl">The base URL.</param>
    /// <param name="jsonOptions">The JSON serializer options.</param>
    /// <param name="cancellationToken">The cancellation token (will be extracted from args).</param>
    /// <returns>An HttpRequestMessage and timeout configuration.</returns>
    public static (HttpRequestMessage Request, TimeSpan? Timeout) BuildRequest(
        MethodInfo method,
        object?[] args,
        string baseUrl,
        JsonSerializerOptions jsonOptions,
        out CancellationToken cancellationToken)
    {
        cancellationToken = default;
        
        var httpMethodAttr = method.GetCustomAttribute<HttpMethodAttribute>();
        if (httpMethodAttr == null)
        {
            throw new InvalidOperationException(
                $"Method {method.Name} must have an HTTP method attribute (Get, Post, Put, Delete, Patch)");
        }

        var httpMethod = httpMethodAttr.Method;
        var path = httpMethodAttr.Path;
        TimeSpan? methodTimeout = httpMethodAttr.TimeoutSeconds.HasValue 
            ? TimeSpan.FromSeconds(httpMethodAttr.TimeoutSeconds.Value) 
            : null;

        var parameters = method.GetParameters();
        var routeParams = new Dictionary<string, string>();
        var queryParams = new List<(string Name, string Value)>();
        var headers = new Dictionary<string, string>();
        object? bodyContent = null;
        FileContent? fileContent = null;
        MultipartContent? multipartContent = null;
        bool isFormEncoded = false;

        // Process parameters
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var value = args[i];

            // Handle CancellationToken
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                cancellationToken = value is CancellationToken ct ? ct : default;
                continue;
            }

            if (value == null)
                continue;

            var bodyAttr = parameter.GetCustomAttribute<BodyAttribute>();
            var headerAttr = parameter.GetCustomAttribute<HeaderAttribute>();
            var queryAttr = parameter.GetCustomAttribute<QueryAttribute>();
            var multipartAttr = parameter.GetCustomAttribute<MultipartAttribute>();
            var formAttr = parameter.GetCustomAttribute<FormAttribute>();

            if (bodyAttr != null)
            {
                bodyContent = value;
            }
            else if (headerAttr != null)
            {
                var headerName = headerAttr.Name ?? parameter.Name ?? $"param{i}";
                headers[headerName] = value.ToString() ?? string.Empty;
            }
            else if (queryAttr != null)
            {
                // FIXED: Properly handle Query parameters
                var queryName = queryAttr.Name ?? parameter.Name ?? $"param{i}";
                if (value is System.Collections.IEnumerable enumerable && value is not string)
                {
                    // Handle collections
                    foreach (var item in enumerable)
                    {
                        if (item != null)
                        {
                            queryParams.Add((queryName, item.ToString() ?? string.Empty));
                        }
                    }
                }
                else
                {
                    queryParams.Add((queryName, value.ToString() ?? string.Empty));
                }
            }
            else if (multipartAttr != null)
            {
                // Handle file upload
                if (value is FileContent fc)
                {
                    fileContent = fc;
                }
                else if (value is MultipartContent mc)
                {
                    multipartContent = mc;
                }
                else if (value is Stream stream)
                {
                    fileContent = FileContent.FromStream(
                        parameter.Name ?? "file", 
                        stream);
                }
                else if (value is byte[] bytes)
                {
                    fileContent = FileContent.FromBytes(
                        parameter.Name ?? "file", 
                        bytes);
                }
            }
            else if (formAttr != null)
            {
                bodyContent = value;
                isFormEncoded = true;
            }
            else
            {
                // Default: route parameter (NOT query parameter!)
                var paramName = parameter.Name ?? $"param{i}";
                
                // Check if this parameter is actually used in the route
                if (path.Contains($"{{{paramName}}}"))
                {
                    routeParams[paramName] = value.ToString() ?? string.Empty;
                }
                else
                {
                    // If not in route and no attribute, treat as query parameter
                    queryParams.Add((paramName, value.ToString() ?? string.Empty));
                }
            }
        }

        // Build the URL
        var url = BuildUrl(baseUrl, path, routeParams, queryParams);

        // Create the request
        var request = new HttpRequestMessage(httpMethod, url);

        // Add headers
        foreach (var header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Add body content
        if (multipartContent != null)
        {
            // Multipart form data with multiple files and fields
            var content = new MultipartFormDataContent();
            
            foreach (var file in multipartContent.Files)
            {
                var fileStream = file.Content ?? new MemoryStream(file.ContentBytes ?? Array.Empty<byte>());
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(streamContent, "files", file.FileName);
            }
            
            foreach (var field in multipartContent.Fields)
            {
                content.Add(new StringContent(field.Value), field.Key);
            }
            
            request.Content = content;
        }
        else if (fileContent != null)
        {
            // Single file upload
            var content = new MultipartFormDataContent();
            var fileStream = fileContent.Content ?? new MemoryStream(fileContent.ContentBytes ?? Array.Empty<byte>());
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(fileContent.ContentType);
            content.Add(streamContent, "file", fileContent.FileName);
            request.Content = content;
        }
        else if (bodyContent != null)
        {
            if (isFormEncoded)
            {
                // Form URL-encoded content
                var formData = ConvertToFormUrlEncoded(bodyContent);
                request.Content = new FormUrlEncodedContent(formData);
            }
            else
            {
                // JSON content
                var json = JsonSerializer.Serialize(bodyContent, jsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
        }

        return (request, methodTimeout);
    }

    /// <summary>
    /// Builds the complete URL with route and query parameters.
    /// </summary>
    private static string BuildUrl(
        string baseUrl,
        string path,
        Dictionary<string, string> routeParams,
        List<(string Name, string Value)> queryParams)
    {
        // Remove trailing slash from base URL
        baseUrl = baseUrl.TrimEnd('/');

        // Replace route parameters
        var processedPath = RouteParameterRegex.Replace(path, match =>
        {
            var paramName = match.Groups[1].Value;
            if (routeParams.TryGetValue(paramName, out var value))
            {
                return Uri.EscapeDataString(value);
            }
            throw new InvalidOperationException($"Route parameter '{paramName}' was not provided");
        });

        // Ensure path starts with /
        if (!processedPath.StartsWith('/'))
        {
            processedPath = "/" + processedPath;
        }

        // Build the complete URL
        var url = baseUrl + processedPath;

        // Add query parameters
        if (queryParams.Any())
        {
            var queryString = string.Join("&",
                queryParams.Select(q => $"{Uri.EscapeDataString(q.Name)}={Uri.EscapeDataString(q.Value)}"));
            url += "?" + queryString;
        }

        return url;
    }

    /// <summary>
    /// Converts an object to form URL-encoded key-value pairs.
    /// </summary>
    private static Dictionary<string, string> ConvertToFormUrlEncoded(object obj)
    {
        var result = new Dictionary<string, string>();
        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj);
            if (value != null)
            {
                result[prop.Name] = value.ToString() ?? string.Empty;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the return type from a method, handling Task, Task&lt;T&gt;, and ValueTask&lt;T&gt;.
    /// </summary>
    public static Type GetReturnType(MethodInfo method)
    {
        var returnType = method.ReturnType;

        // Handle Task
        if (returnType == typeof(Task))
        {
            return typeof(void);
        }

        // Handle Task<T>
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return returnType.GetGenericArguments()[0];
        }

        // Handle ValueTask<T>
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            return returnType.GetGenericArguments()[0];
        }

        throw new InvalidOperationException(
            $"Method {method.Name} must return Task, Task<T>, or ValueTask<T>");
    }

    /// <summary>
    /// Checks if the method expects an ApiResponse wrapper.
    /// </summary>
    public static bool IsApiResponseType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ApiResponse<>);
    }

    /// <summary>
    /// Gets the inner type from ApiResponse&lt;T&gt;.
    /// </summary>
    public static Type GetApiResponseInnerType(Type apiResponseType)
    {
        return apiResponseType.GetGenericArguments()[0];
    }
}
