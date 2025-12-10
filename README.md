# CoreApiClient v1.2 - Production-Ready HTTP API Client

[![NuGet](https://img.shields.io/nuget/v/RoyceLark.ApiClient.svg)](https://www.nuget.org/packages/RoyceLark.ApiClient/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A modern, production-grade HTTP API client library for .NET that generates clients at runtime from interface definitions. **Complete with file uploads, enhanced downloads with progress tracking, and enterprise features!**

## 🚀 What's New in v1.2

### 📥 **Enhanced Download Features** (NEW!)
✨ **Progress Reporting** - Real-time download progress  
✨ **Stream to File** - Memory-efficient downloads  
✨ **Range Downloads** - Download specific byte ranges  
✨ **Resume Support** - Auto-resume interrupted downloads  
✨ **Pre-Download Info** - Check file size before downloading  
✨ **Cancellation** - Cancel downloads anytime  

### 📤 **Upload & Core Features**
✨ **File Upload** - Single/multiple files (v1.1)  
✨ **CancellationToken** - Cancel requests (v1.1)  
✨ **Response Headers** - Access metadata (v1.1)  
✨ **Per-Request Timeout** - Override per endpoint (v1.1)  

---

## 📦 Installation

```bash
dotnet add package RoyceLark.ApiClient
```

---

## 🎯 Quick Start

```csharp
using RoyceLark.ApiClient;
using RoyceLark.ApiClient.Attributes;

public interface IUserApi
{
    [Get("/users/{id}")]
    Task<User> GetUser(int id);
    
    [Post("/upload")]
    Task<string> UploadFile([Multipart] FileContent file);
}

var api = ApiClient.Create<IUserApi>("https://api.example.com");
var user = await api.GetUser(123);
```

---

## 📥 **Download Guide** (Complete)

### **1. Download with Progress** ⭐

```csharp
using RoyceLark.ApiClient.Extensions;

var httpClient = new HttpClient();

var progress = new Progress<DownloadProgress>(p =>
{
    Console.WriteLine($"{p.ProgressPercentage:F1}% - {p.BytesDownloadedFormatted}");
});

var result = await httpClient.DownloadFileAsync(
    "https://example.com/file.zip",
    progress);
```

### **2. Stream to File** ⭐

```csharp
var result = await httpClient.DownloadToFileAsync(
    "https://example.com/video.mp4",
    "/path/to/video.mp4",
    progress);

Console.WriteLine($"Saved: {result.SizeFormatted} to {result.FilePath}");
```

### **3. Resume Downloads** ⭐

```csharp
var result = await httpClient.DownloadWithResumeAsync(
    url: "https://example.com/huge.zip",
    filePath: "huge.zip",
    progress: progress,
    maxRetries: 5);
```

### **4. Range Downloads** ⭐

```csharp
// Download bytes 1000-2000
var result = await httpClient.DownloadRangeAsync(url, 1000, 2000);

// Download from 5000 to end
var result = await httpClient.DownloadRangeAsync(url, 5000);
```

### **5. Pre-Download Info** ⭐

```csharp
// Check file size
var size = await httpClient.GetFileSizeAsync(url);
Console.WriteLine($"File size: {size} bytes");

// Check resume support
var supportsResume = await httpClient.SupportsRangeDownloadsAsync(url);
```

### **6. With Cancellation** ⭐

```csharp
using var cts = new CancellationTokenSource();

var result = await httpClient.DownloadToFileAsync(
    url, 
    path, 
    progress, 
    cts.Token);
```

---

## 📤 **Upload Guide** (Complete)

### **1. Single File**

```csharp
[Post("/upload")]
Task<string> Upload([Multipart] FileContent file);

// From file
var file = FileContent.FromFile("document.pdf");
await api.Upload(file);

// From bytes
var file = FileContent.FromBytes("file.jpg", bytes, "image/jpeg");
await api.Upload(file);

// From stream
var file = FileContent.FromStream("video.mp4", stream, "video/mp4");
await api.Upload(file);
```

### **2. Multiple Files**

```csharp
[Post("/upload-multiple")]
Task<string> UploadMultiple([Multipart] MultipartContent files);

var content = new MultipartContent();
content.AddFile(FileContent.FromFile("file1.pdf"));
content.AddFile(FileContent.FromFile("file2.jpg"));
content.AddField("description", "My files");
await api.UploadMultiple(content);
```

---

## 🎯 **Core Features**

### **CancellationToken**

```csharp
[Get("/users")]
Task<List<User>> GetUsers(CancellationToken ct = default);

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var users = await api.GetUsers(cts.Token);
```

### **Response Headers**

```csharp
[Get("/users/{id}")]
Task<ApiResponse<User>> GetUser(int id);

var response = await api.GetUser(123);
Console.WriteLine($"ETag: {response.GetHeader("ETag")}");
var user = response.Content;
```

### **Per-Request Timeout**

```csharp
[Get("/users/{id}", TimeoutSeconds = 5)]
Task<User> GetUserFast(int id);

[Get("/reports", TimeoutSeconds = 300)]
Task<Report> GenerateReport();
```

### **Query Parameters**

```csharp
[Get("/users")]
Task<List<User>> Search(
    [Query] string search,
    [Query] int page = 1);

// Generates: /users?search=john&page=1
var users = await api.Search("john", 1);
```

### **Form Data**

```csharp
[Post("/login")]
Task<Token> Login([Form] LoginData data);

var token = await api.Login(new LoginData 
{ 
    Username = "user", 
    Password = "pass" 
});
```

---

## 📚 **Complete Features**

### HTTP Methods
`[Get]` `[Post]` `[Put]` `[Delete]` `[Patch]`

### Parameter Attributes
`[Body]` `[Query]` `[Header]` `[Multipart]` `[Form]`

### Return Types
`Task` `Task<T>` `ValueTask<T>` `Task<ApiResponse<T>>`

### Download Methods (HttpClient Extensions)
- `DownloadFileAsync()` - With progress
- `DownloadToFileAsync()` - Stream to disk
- `DownloadRangeAsync()` - Partial download
- `DownloadWithResumeAsync()` - Auto-resume
- `GetFileSizeAsync()` - Check size
- `SupportsRangeDownloadsAsync()` - Check resume

### Configuration
- ✅ Retry policies (Polly)
- ✅ Logging
- ✅ HttpClientFactory
- ✅ Dependency injection
- ✅ Custom JSON
- ✅ Timeouts
- ✅ Headers

---

## 🏗️ **Dependency Injection**

```csharp
services.AddApiClient<IUserApi>(options =>
{
    options.BaseUrl = "https://api.example.com";
    options.Timeout = TimeSpan.FromSeconds(30);
    options.RetryPolicy.MaxRetryAttempts = 3;
});
```

---

## ⚠️ **Error Handling**

```csharp
try
{
    var user = await api.GetUser(999);
}
catch (ApiException ex)
{
    Console.WriteLine($"Status: {ex.StatusCode}");
    Console.WriteLine($"Content: {ex.Content}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Cancelled");
}
catch (TimeoutException)
{
    Console.WriteLine("Timeout");
}
```

---

## 📊 **Comparison**

| Feature | CoreApiClient | Refit | RestSharp |
|---------|--------------|-------|-----------|
| Interface-based | ✅ | ✅ | ❌ |
| Download progress | ✅ | ❌ | ⚠️ |
| Stream to file | ✅ | ❌ | ⚠️ |
| Resume downloads | ✅ | ❌ | ❌ |
| Per-request timeout | ✅ | ❌ | ✅ |
| Built-in retry | ✅ | Extension | ✅ |

---

## 📝 **Version History**

- **v1.2.0** (2025-12-10): Enhanced downloads (progress, resume, streaming)
- **v1.1.0** (2025-12-10): File upload, CancellationToken, response headers
- **v1.0.0** (2025-12-09): Initial release

---

## 🎯 **Summary**

RoyceLark.ApiClient provides:

✅ Complete HTTP client functionality  
✅ File upload (all sources)  
✅ Enhanced downloads (progress, resume)  
✅ CancellationToken support  
✅ Response headers access  
✅ Retry & timeout policies  
✅ Production-ready  

**Everything you need for HTTP APIs!** 🚀

---

## 📄 License

MIT License

## 🔗 Links

- [GitHub](https://github.com/roycelark/CoreApiClient)
- [NuGet](https://www.nuget.org/packages/CoreApiClient)
- [Changelog](CHANGELOG.md)

---

**Made with ❤️ for the .NET community**