# CoreApiClient Architecture

This document provides a deep dive into the architecture and design decisions of CoreApiClient.

## Overview

CoreApiClient uses a dynamic proxy pattern based on `System.Reflection.DispatchProxy` to intercept method calls on interface definitions and convert them into HTTP requests at runtime.

## Core Components

### 1. ApiClient (Factory)

**Location**: `ApiClient.cs`

The main entry point that creates API client instances. It:
- Validates that the generic type is an interface
- Validates the interface structure (methods, attributes)
- Creates a DispatchProxy instance
- Configures the HttpClient
- Returns a strongly-typed proxy

```
User Code → ApiClient.Create<T>() → DispatchProxy.Create<T, ApiClientProxy<T>>()
```

### 2. ApiClientProxy (Dynamic Proxy)

**Location**: `Internal/ApiClientProxy.cs`

The heart of the library. This class:
- Inherits from `DispatchProxy`
- Intercepts all method calls via `Invoke()`
- Delegates to RequestBuilder for request construction
- Executes HTTP requests via HttpClient
- Deserializes responses using System.Text.Json
- Handles errors and logging

**Key Methods**:
- `Initialize()`: Sets up dependencies (HttpClient, options, logger)
- `Invoke()`: Intercepts method calls
- `InvokeAsync<T>()`: Executes async methods with return values
- `ExecuteRequestAsync<T>()`: Performs HTTP request and deserialization

### 3. RequestBuilder

**Location**: `Internal/RequestBuilder.cs`

Responsible for building HttpRequestMessage from method metadata. It:
- Reads HTTP method attributes (Get, Post, Put, Delete)
- Reads parameter attributes (Body, Query, Header)
- Replaces route parameters using regex
- Constructs query strings
- Serializes request bodies to JSON
- Sets HTTP headers

**Key Methods**:
- `BuildRequest()`: Main entry point, creates HttpRequestMessage
- `BuildUrl()`: Constructs complete URL with parameters
- `GetReturnType()`: Extracts return type from Task<T>

### 4. Attributes

**Location**: `Attributes/HttpAttributes.cs`

Declarative attributes that define API structure:

```csharp
[Get("/users/{id}")]        // HTTP method + route
Task<User> GetUser(
    int id,                  // Route parameter (default)
    [Query] int? page,       // Query string parameter
    [Header("Auth")] string token, // HTTP header
    [Body] User user         // Request body (JSON)
);
```

### 5. Configuration

**Location**: `Configuration/ApiClientOptions.cs`

Central configuration system:
- Base URL
- Timeouts
- Retry policies
- JSON serialization options
- Logging settings
- Default headers

### 6. Extensions

**Location**: `Extensions/ServiceCollectionExtensions.cs`

Dependency injection integration:
- Registers API clients in DI container
- Configures HttpClientFactory
- Sets up Polly retry policies
- Binds configuration from appsettings.json

## Data Flow

### 1. Client Creation Flow

```
User calls ApiClient.Create<IUserApi>()
  ↓
Validate interface structure
  ↓
Create DispatchProxy instance
  ↓
Initialize proxy with HttpClient, options, logger
  ↓
Return proxy cast to IUserApi
```

### 2. Method Invocation Flow

```
User calls api.GetUser(123)
  ↓
DispatchProxy.Invoke() intercepts call
  ↓
RequestBuilder.BuildRequest() creates HttpRequestMessage
  ↓
Execute HTTP request via HttpClient
  ↓
Log request/response (if enabled)
  ↓
Check response status code
  ↓
Deserialize JSON response to User object
  ↓
Return User to caller
```

### 3. Error Handling Flow

```
HTTP request fails
  ↓
Check if retry policy applies
  ↓
If retryable: wait and retry (via Polly)
  ↓
If not retryable or max retries exceeded:
  ↓
Create ApiException with full context
  ↓
Log error
  ↓
Throw ApiException to caller
```

## Design Patterns

### 1. Proxy Pattern

Uses `DispatchProxy` to create runtime proxies that implement interface methods.

**Benefits**:
- Zero boilerplate code
- Compile-time type safety
- IntelliSense support

### 2. Factory Pattern

`ApiClient.Create<T>()` is a factory method that encapsulates object creation logic.

### 3. Strategy Pattern

Different strategies for:
- HTTP method execution (GET, POST, PUT, DELETE)
- Retry policies (exponential backoff, linear)
- Error handling (throw vs suppress)

### 4. Decorator Pattern

Polly policies wrap HttpClient calls with retry, timeout, and circuit breaker logic.

## Extensibility Points

CoreApiClient is designed for extensibility through several mechanisms:

### 1. Custom Attributes (Future)

```csharp
[AttributeUsage(AttributeTargets.Parameter)]
public class PathAttribute : Attribute
{
    public string Name { get; }
    public PathAttribute(string name) => Name = name;
}
```

### 2. Interceptors (Future)

```csharp
public interface IRequestInterceptor
{
    Task OnRequestAsync(HttpRequestMessage request);
    Task OnResponseAsync(HttpResponseMessage response);
}
```

### 3. Custom Serializers (Current)

```csharp
options.JsonSerializerOptions = new JsonSerializerOptions
{
    Converters = { new MyCustomConverter() }
};
```

### 4. Telemetry Hooks (Future)

```csharp
options.EnableTelemetry = true;
options.TelemetryProvider = new OpenTelemetryProvider();
```

## Performance Considerations

### 1. Reflection Overhead

- Method metadata is read via reflection on each call
- **Future optimization**: Cache MethodInfo and attributes

### 2. Memory Allocation

- Each request creates new HttpRequestMessage
- JSON serialization allocates strings
- **Current mitigation**: Object pooling in .NET runtime

### 3. HttpClient Reuse

- HttpClient instances should be reused
- HttpClientFactory manages lifecycle
- Connection pooling enabled by default

### 4. Async/Await

- All operations are async to avoid thread blocking
- Uses ConfigureAwait(false) to avoid context capturing

## Security Considerations

### 1. Input Validation

- Route parameters are URL-encoded
- Query parameters are URL-encoded
- Headers are validated by HttpClient

### 2. HTTPS Enforcement

- No automatic enforcement (user responsibility)
- Recommended: Always use HTTPS in production

### 3. Credential Management

- No credentials stored in library
- User provides via headers or DI configuration

### 4. Request/Response Logging

- Sensitive data may be logged
- Can disable via `LogRequestBody = false`

## Testing Strategy

### 1. Unit Tests

Test individual components in isolation:
- RequestBuilder
- Attribute parsing
- URL construction
- Error handling

### 2. Integration Tests

Test full flow with mock HTTP:
- RichardSzalay.MockHttp for HTTP mocking
- Full request/response cycle
- Retry logic
- Error scenarios

### 3. Example Application

Real-world usage against live API (JSONPlaceholder):
- Validates actual HTTP behavior
- Demonstrates usage patterns
- Serves as documentation

## Dependencies

### Core Dependencies

1. **Microsoft.Extensions.Http** (8.0.1)
   - HttpClientFactory support
   - Typed HTTP clients

2. **Microsoft.Extensions.Http.Polly** (8.0.11)
   - Integration between HttpClient and Polly
   - Retry policies

3. **Polly** (8.5.0)
   - Resilience and transient-fault-handling
   - Retry, timeout, circuit breaker

4. **System.Text.Json** (8.0.5)
   - JSON serialization/deserialization
   - High performance

5. **Microsoft.Extensions.Logging.Abstractions** (8.0.2)
   - Logging infrastructure
   - Provider-agnostic

### Why These Choices?

- **System.Text.Json** vs Newtonsoft.Json: Better performance, smaller size, built-in
- **Polly** vs custom: Battle-tested, feature-rich, widely adopted
- **DispatchProxy** vs other proxies: Built-in, zero dependencies, good performance

## Comparison with Alternatives

### vs. Refit

| Aspect | CoreApiClient | Refit |
|--------|---------------|-------|
| Proxy generation | Runtime (DispatchProxy) | Compile-time (Source Generators) |
| Setup complexity | Lower | Higher |
| Compile-time safety | Same | Same |
| Runtime overhead | Minimal (reflection) | None |
| Extensibility | High | Medium |
| Package size | Smaller | Larger |

### vs. HttpClient Direct

| Aspect | CoreApiClient | HttpClient |
|--------|---------------|------------|
| Boilerplate | Minimal | Significant |
| Type safety | Strong | Weak (strings) |
| Maintainability | High | Medium |
| Flexibility | High | Highest |
| Learning curve | Low | Medium |

## Future Enhancements

### Short Term (v1.1-1.5)

1. **Response caching**: Attribute-based HTTP caching
2. **File upload/download**: Multipart form data support
3. **GraphQL support**: Query and mutation attributes
4. **Source generation**: Optional compile-time generation for zero reflection

### Medium Term (v2.0)

1. **Circuit breaker**: Advanced resilience patterns
2. **Rate limiting**: Client-side rate limiting
3. **Request/Response middleware**: Pipeline for transformations
4. **Mocking framework**: Built-in mock API support

### Long Term (v3.0+)

1. **gRPC support**: Beyond REST
2. **SignalR integration**: Real-time APIs
3. **API versioning**: Built-in version management
4. **Code-first OpenAPI**: Generate OpenAPI specs from interfaces

## Conclusion

CoreApiClient's architecture balances simplicity, performance, and extensibility. The use of DispatchProxy enables a clean API with minimal overhead, while the modular design allows for future enhancements without breaking changes.

The library is production-ready and suitable for:
- Microservices communication
- External API integration
- Mobile app backends
- Any scenario requiring type-safe HTTP clients
