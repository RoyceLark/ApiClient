using System.Net;

namespace CoreApiClient.Models;

/// <summary>
/// Represents an API response with headers and status information.
/// </summary>
/// <typeparam name="T">The response content type.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Gets the response content.
    /// </summary>
    public T? Content { get; init; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; init; }

    /// <summary>
    /// Gets the response headers.
    /// </summary>
    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; init; } = 
        new Dictionary<string, IEnumerable<string>>();

    /// <summary>
    /// Gets a value indicating whether the response was successful.
    /// </summary>
    public bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode <= 299;

    /// <summary>
    /// Gets the reason phrase.
    /// </summary>
    public string? ReasonPhrase { get; init; }

    /// <summary>
    /// Gets a specific header value.
    /// </summary>
    /// <param name="headerName">The header name.</param>
    /// <returns>The header value if found; otherwise, null.</returns>
    public string? GetHeader(string headerName)
    {
        return Headers.TryGetValue(headerName, out var values) 
            ? values.FirstOrDefault() 
            : null;
    }

    /// <summary>
    /// Gets all values for a specific header.
    /// </summary>
    /// <param name="headerName">The header name.</param>
    /// <returns>All header values if found; otherwise, an empty collection.</returns>
    public IEnumerable<string> GetHeaders(string headerName)
    {
        return Headers.TryGetValue(headerName, out var values) 
            ? values 
            : Enumerable.Empty<string>();
    }
}

/// <summary>
/// Represents a file to be uploaded.
/// </summary>
public class FileContent
{
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Gets or sets the file content stream.
    /// </summary>
    public Stream? Content { get; set; }

    /// <summary>
    /// Gets or sets the file content as bytes.
    /// </summary>
    public byte[]? ContentBytes { get; set; }

    /// <summary>
    /// Creates a FileContent from a file path.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="contentType">The content type. If null, will be inferred from extension.</param>
    /// <returns>A new FileContent instance.</returns>
    public static FileContent FromFile(string filePath, string? contentType = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}", filePath);
        }

        var fileName = Path.GetFileName(filePath);
        contentType ??= GetContentTypeFromExtension(Path.GetExtension(filePath));

        return new FileContent
        {
            FileName = fileName,
            ContentType = contentType,
            Content = File.OpenRead(filePath)
        };
    }

    /// <summary>
    /// Creates a FileContent from a byte array.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="content">The file content.</param>
    /// <param name="contentType">The content type.</param>
    /// <returns>A new FileContent instance.</returns>
    public static FileContent FromBytes(string fileName, byte[] content, string contentType = "application/octet-stream")
    {
        return new FileContent
        {
            FileName = fileName,
            ContentType = contentType,
            ContentBytes = content
        };
    }

    /// <summary>
    /// Creates a FileContent from a stream.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="stream">The file content stream.</param>
    /// <param name="contentType">The content type.</param>
    /// <returns>A new FileContent instance.</returns>
    public static FileContent FromStream(string fileName, Stream stream, string contentType = "application/octet-stream")
    {
        return new FileContent
        {
            FileName = fileName,
            ContentType = contentType,
            Content = stream
        };
    }

    private static string GetContentTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".zip" => "application/zip",
            ".csv" => "text/csv",
            _ => "application/octet-stream"
        };
    }
}

/// <summary>
/// Represents multiple files to be uploaded together.
/// </summary>
public class MultipartContent
{
    /// <summary>
    /// Gets or sets the files to upload.
    /// </summary>
    public List<FileContent> Files { get; set; } = new();

    /// <summary>
    /// Gets or sets additional form fields.
    /// </summary>
    public Dictionary<string, string> Fields { get; set; } = new();

    /// <summary>
    /// Adds a file to the multipart content.
    /// </summary>
    /// <param name="file">The file to add.</param>
    public void AddFile(FileContent file)
    {
        Files.Add(file);
    }

    /// <summary>
    /// Adds a field to the multipart content.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="value">The field value.</param>
    public void AddField(string name, string value)
    {
        Fields[name] = value;
    }
}
