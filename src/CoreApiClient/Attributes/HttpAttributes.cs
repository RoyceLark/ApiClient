namespace CoreApiClient.Attributes;

/// <summary>
/// Base attribute for HTTP method attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public abstract class HttpMethodAttribute : Attribute
{
    /// <summary>
    /// Gets the HTTP method.
    /// </summary>
    public HttpMethod Method { get; }

    /// <summary>
    /// Gets the route template.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets or sets the timeout for this specific request in seconds. If null, uses the default timeout.
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Initializes a new instance of the HttpMethodAttribute class.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The route template.</param>
    protected HttpMethodAttribute(HttpMethod method, string path)
    {
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }
}

/// <summary>
/// Indicates that the method should perform an HTTP GET request.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class GetAttribute : HttpMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the GetAttribute class.
    /// </summary>
    /// <param name="path">The route template.</param>
    public GetAttribute(string path) : base(HttpMethod.Get, path)
    {
    }
}

/// <summary>
/// Indicates that the method should perform an HTTP POST request.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class PostAttribute : HttpMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the PostAttribute class.
    /// </summary>
    /// <param name="path">The route template.</param>
    public PostAttribute(string path) : base(HttpMethod.Post, path)
    {
    }
}

/// <summary>
/// Indicates that the method should perform an HTTP PUT request.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class PutAttribute : HttpMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the PutAttribute class.
    /// </summary>
    /// <param name="path">The route template.</param>
    public PutAttribute(string path) : base(HttpMethod.Put, path)
    {
    }
}

/// <summary>
/// Indicates that the method should perform an HTTP DELETE request.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class DeleteAttribute : HttpMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the DeleteAttribute class.
    /// </summary>
    /// <param name="path">The route template.</param>
    public DeleteAttribute(string path) : base(HttpMethod.Delete, path)
    {
    }
}

/// <summary>
/// Indicates that the method should perform an HTTP PATCH request.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class PatchAttribute : HttpMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the PatchAttribute class.
    /// </summary>
    /// <param name="path">The route template.</param>
    public PatchAttribute(string path) : base(HttpMethod.Patch, path)
    {
    }
}

/// <summary>
/// Indicates that the parameter should be sent in the request body as JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public class BodyAttribute : Attribute
{
}

/// <summary>
/// Indicates that the parameter should be sent as an HTTP header.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public class HeaderAttribute : Attribute
{
    /// <summary>
    /// Gets the header name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Initializes a new instance of the HeaderAttribute class.
    /// </summary>
    /// <param name="name">The header name. If null, the parameter name will be used.</param>
    public HeaderAttribute(string? name = null)
    {
        Name = name;
    }
}

/// <summary>
/// Indicates that the parameter should be sent as a query string parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public class QueryAttribute : Attribute
{
    /// <summary>
    /// Gets the query parameter name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Initializes a new instance of the QueryAttribute class.
    /// </summary>
    /// <param name="name">The query parameter name. If null, the parameter name will be used.</param>
    public QueryAttribute(string? name = null)
    {
        Name = name;
    }
}

/// <summary>
/// Indicates that the parameter should be sent as a multipart/form-data file.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public class MultipartAttribute : Attribute
{
    /// <summary>
    /// Gets the form field name for the file.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Initializes a new instance of the MultipartAttribute class.
    /// </summary>
    /// <param name="name">The form field name. If null, the parameter name will be used.</param>
    public MultipartAttribute(string? name = null)
    {
        Name = name;
    }
}

/// <summary>
/// Indicates that the parameter should be sent as application/x-www-form-urlencoded data.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public class FormAttribute : Attribute
{
}

/// <summary>
/// Indicates that the method response should include HTTP headers.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class IncludeHeadersAttribute : Attribute
{
}
