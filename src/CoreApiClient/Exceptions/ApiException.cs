using System.Net;

namespace CoreApiClient.Exceptions;

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? Content { get; }
    public Uri? RequestUri { get; }
    public HttpMethod? Method { get; }
    public IDictionary<string, IEnumerable<string>>? Headers { get; }

    public ApiException(string message, HttpStatusCode statusCode, string? content = null, Uri? requestUri = null,
        HttpMethod? method = null, IDictionary<string, IEnumerable<string>>? headers = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Content = content;
        RequestUri = requestUri;
        Method = method;
        Headers = headers;
    }

    public static async Task<ApiException> CreateAsync(HttpResponseMessage response, string? content = null)
    {
        content ??= await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var headers = response.Headers.Concat(response.Content.Headers).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var message = $"API request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase})";
        return new ApiException(message, response.StatusCode, content, response.RequestMessage?.RequestUri,
            response.RequestMessage?.Method, headers);
    }
}

public class ApiConfigurationException : Exception
{
    public ApiConfigurationException(string message, Exception? innerException = null) : base(message, innerException) { }
}

public class ValidationException : Exception
{
    public IDictionary<string, string[]>? Errors { get; }
    public ValidationException(string message, IDictionary<string, string[]>? errors = null, Exception? innerException = null)
        : base(message, innerException) { Errors = errors; }
}
