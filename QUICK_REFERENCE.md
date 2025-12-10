# CoreApiClient Quick Reference

## Installation
```bash
dotnet add package CoreApiClient
```

## Basic Usage

### 1. Define Interface
```csharp
using CoreApiClient.Attributes;

public interface IUserApi
{
    [Get("/users/{id}")]
    Task<User> GetUser(int id);
    
    [Post("/users")]
    Task<User> CreateUser([Body] User user);
}
```

### 2. Create Client
```csharp
var api = ApiClient.Create<IUserApi>("https://api.example.com");
```

### 3. Make Requests
```csharp
var user = await api.GetUser(123);
var newUser = await api.CreateUser(new User { Name = "John" });
```

## HTTP Attributes

| Attribute | Usage | Example |
|-----------|-------|---------|
| `[Get(path)]` | HTTP GET | `[Get("/users/{id}")]` |
| `[Post(path)]` | HTTP POST | `[Post("/users")]` |
| `[Put(path)]` | HTTP PUT | `[Put("/users/{id}")]` |
| `[Delete(path)]` | HTTP DELETE | `[Delete("/users/{id}")]` |

## Parameter Attributes

| Attribute | Location | Example |
|-----------|----------|---------|
| Default | Route parameter | `Task<User> Get(int id)` |
| `[Body]` | Request body (JSON) | `Task<User> Create([Body] User u)` |
| `[Query]` | Query string | `Task<List<User>> Get([Query] int page)` |
| `[Header]` | HTTP header | `Task<User> Get([Header("Auth")] string token)` |

## Configuration

### Simple
```csharp
var api = ApiClient.Create<IUserApi>("https://api.example.com", options =>
{
    options.Timeout = TimeSpan.FromSeconds(30);
    options.DefaultHeaders.Add("Authorization", "Bearer token");
});
```

### Full Configuration
```csharp
var api = ApiClient.Create<IUserApi>("https://api.example.com", options =>
{
    // Timeouts
    options.Timeout = TimeSpan.FromSeconds(30);
    
    // Headers
    options.DefaultHeaders.Add("Authorization", "Bearer token");
    options.DefaultHeaders.Add("User-Agent", "MyApp/1.0");
    
    // Retry Policy
    options.RetryPolicy.Enabled = true;
    options.RetryPolicy.MaxRetryAttempts = 3;
    options.RetryPolicy.UseExponentialBackoff = true;
    options.RetryPolicy.RetryDelay = TimeSpan.FromSeconds(1);
    
    // Logging
    options.EnableLogging = true;
    options.LogRequestBody = true;
    options.LogResponseBody = false;
    
    // JSON
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    
    // Error Handling
    options.ThrowOnError = true;
});
```

## Dependency Injection

### Setup
```csharp
// In Startup.cs or Program.cs
services.AddApiClient<IUserApi>(options =>
{
    options.BaseUrl = "https://api.example.com";
});
```

### Usage
```csharp
public class UserService
{
    private readonly IUserApi _api;
    
    public UserService(IUserApi api)
    {
        _api = api;
    }
    
    public async Task<User> GetUserAsync(int id)
    {
        return await _api.GetUser(id);
    }
}
```

## Error Handling

### With Exception
```csharp
try
{
    var user = await api.GetUser(999);
}
catch (ApiException ex)
{
    Console.WriteLine($"Status: {ex.StatusCode}");
    Console.WriteLine($"Content: {ex.Content}");
    Console.WriteLine($"URI: {ex.RequestUri}");
}
```

### Without Exception
```csharp
var api = ApiClient.Create<IUserApi>("...", options =>
{
    options.ThrowOnError = false;
});

var user = await api.GetUser(999); // Returns null instead of throwing
```

## Common Patterns

### CRUD Operations
```csharp
public interface ICrudApi<T>
{
    [Get("/{resource}/{id}")]
    Task<T> Get(string resource, int id);
    
    [Get("/{resource}")]
    Task<List<T>> GetAll(string resource);
    
    [Post("/{resource}")]
    Task<T> Create(string resource, [Body] T item);
    
    [Put("/{resource}/{id}")]
    Task<T> Update(string resource, int id, [Body] T item);
    
    [Delete("/{resource}/{id}")]
    Task Delete(string resource, int id);
}
```

### Pagination
```csharp
[Get("/users")]
Task<List<User>> GetUsers(
    [Query] int page = 1,
    [Query] int pageSize = 10);
```

### Search/Filter
```csharp
[Get("/users")]
Task<List<User>> SearchUsers(
    [Query] string? search = null,
    [Query] string? sortBy = null,
    [Query] string? order = null);
```

### Authentication
```csharp
// Option 1: Default header
var api = ApiClient.Create<IUserApi>("...", options =>
{
    options.DefaultHeaders.Add("Authorization", "Bearer token");
});

// Option 2: Per request
[Get("/users/{id}")]
Task<User> GetUser(int id, [Header("Authorization")] string token);
```

## Return Types

| Type | Description | Example |
|------|-------------|---------|
| `Task` | No return value | `[Delete("/users/{id}")] Task Delete(int id)` |
| `Task<T>` | Strongly typed | `[Get("/users/{id}")] Task<User> Get(int id)` |
| `Task<List<T>>` | Collection | `[Get("/users")] Task<List<User>> GetAll()` |
| `Task<string>` | Raw content | `[Get("/raw")] Task<string> GetRaw()` |
| `ValueTask<T>` | High-perf async | `[Get("/users/{id}")] ValueTask<User> Get(int id)` |

## Testing

### With Moq
```csharp
var mock = new Mock<IUserApi>();
mock.Setup(x => x.GetUser(It.IsAny<int>()))
    .ReturnsAsync(new User { Id = 1, Name = "Test" });
```

### With MockHttp
```csharp
var mockHttp = new MockHttpMessageHandler();
mockHttp.When("https://api.example.com/users/1")
        .Respond("application/json", "{\"id\":1,\"name\":\"Test\"}");

var httpClient = mockHttp.ToHttpClient();
var api = ApiClient.Create<IUserApi>(httpClient, options);
```

## Build & Run

### Build
```bash
dotnet build
```

### Test
```bash
dotnet test
```

### Run Examples
```bash
cd examples/CoreApiClient.Examples
dotnet run
```

### Create Package
```bash
dotnet pack -c Release
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| `Type must be an interface` | Ensure `T` in `ApiClient.Create<T>()` is an interface |
| `Method must have HTTP attribute` | Add `[Get]`, `[Post]`, etc. to method |
| `Method must return Task` | Change return type to `Task` or `Task<T>` |
| `Route parameter not provided` | Ensure parameter exists in method signature |
| `ApiException thrown` | Check status code and response content |

## Best Practices

1. **Reuse client instances** - Create once, use many times
2. **Use HttpClientFactory** - Let DI manage HttpClient lifecycle  
3. **Configure timeouts** - Set appropriate timeouts for your use case
4. **Enable retry policies** - Handle transient failures automatically
5. **Disable body logging in production** - Protect sensitive data
6. **Use strongly-typed models** - Avoid `dynamic` and `object`
7. **Handle exceptions** - Always wrap calls in try-catch
8. **Test with mocks** - Use MockHttp for unit testing

## Resources

- 📖 [Full Documentation](README.md)
- 🏗️ [Architecture Guide](ARCHITECTURE.md)
- 🔨 [Build Instructions](BUILD.md)
- 📋 [Changelog](CHANGELOG.md)
- 💻 [Examples](examples/CoreApiClient.Examples)
- 🐛 [Issues](https://github.com/yourusername/CoreApiClient/issues)
