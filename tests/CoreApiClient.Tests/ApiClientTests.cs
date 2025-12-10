using System.Net;
using System.Text.Json;
using CoreApiClient.Attributes;
using CoreApiClient.Configuration;
using CoreApiClient.Exceptions;
using CoreApiClient.Models;
using FluentAssertions;
using RichardSzalay.MockHttp;
using Xunit;

namespace CoreApiClient.Tests;

public class ApiClientTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    private readonly ApiClientOptions _options;

    public ApiClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _options = new ApiClientOptions
        {
            BaseUrl = "https://api.example.com",
            ThrowOnError = true
        };
    }

    [Fact]
    public void Create_WithNullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ApiClient.Create<ITestApi>(null!, _options));
    }

    [Fact]
    public void Create_WithNullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ApiClient.Create<ITestApi>(_httpClient, null!));
    }

    [Fact]
    public void Create_WithNonInterface_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ApiClient.Create<TestClass>(_httpClient, _options));
    }

    [Fact]
    public async Task GetRequest_WithRouteParameter_BuildsCorrectUrl()
    {
        // Arrange
        var expectedUser = new User { Id = 123, Name = "John Doe" };
        _mockHttp
            .When("https://api.example.com/users/123")
            .Respond("application/json", JsonSerializer.Serialize(expectedUser));

        var client = ApiClient.Create<ITestApi>(_httpClient, _options);

        // Act
        var result = await client.GetUser(123);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(123);
        result.Name.Should().Be("John Doe");
    }

    [Fact]
    public async Task PostRequest_WithBody_SendsCorrectData()
    {
        // Arrange
        var newUser = new User { Name = "Jane Doe", Email = "jane@example.com" };
        var createdUser = new User { Id = 456, Name = "Jane Doe", Email = "jane@example.com" };

        _mockHttp
            .When(HttpMethod.Post, "https://api.example.com/users")
            .WithContent("{\"name\":\"Jane Doe\",\"email\":\"jane@example.com\"}")
            .Respond("application/json", JsonSerializer.Serialize(createdUser));

        var client = ApiClient.Create<ITestApi>(_httpClient, _options);

        // Act
        var result = await client.CreateUser(newUser);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(456);
        result.Name.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task Request_WithQueryParameters_BuildsCorrectUrl()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = 1, Name = "User 1" },
            new User { Id = 2, Name = "User 2" }
        };

        _mockHttp
            .When("https://api.example.com/users?page=1&pageSize=10")
            .Respond("application/json", JsonSerializer.Serialize(users));

        var client = ApiClient.Create<ITestApi>(_httpClient, _options);

        // Act
        var result = await client.GetUsers(1, 10);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Request_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        _mockHttp
            .When("https://api.example.com/users/123")
            .Respond("application/json", JsonSerializer.Serialize(new User { Id = 123 }));

        var client = ApiClient.Create<ITestApi>(_httpClient, _options);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await client.GetUserWithCancellation(123, cts.Token));
    }

    [Fact]
    public async Task Request_WithApiResponse_ReturnsHeadersAndContent()
    {
        // Arrange
        var user = new User { Id = 123, Name = "John" };
        _mockHttp
            .When("https://api.example.com/users/123")
            .Respond(req =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(user))
                };
                response.Headers.Add("X-Custom-Header", "CustomValue");
                response.Headers.Add("ETag", "\"abc123\"");
                return response;
            });

        var client = ApiClient.Create<ITestApi>(_httpClient, _options);

        // Act
        var result = await client.GetUserWithHeaders(123);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.IsSuccessStatusCode.Should().BeTrue();
        result.Content.Should().NotBeNull();
        result.Content!.Id.Should().Be(123);
        result.GetHeader("X-Custom-Header").Should().Be("CustomValue");
        result.GetHeader("ETag").Should().Be("\"abc123\"");
    }

    [Fact]
    public async Task PatchRequest_UpdatesResource()
    {
        // Arrange
        var updatedUser = new User { Id = 123, Name = "Updated Name" };

        _mockHttp
            .When(HttpMethod.Patch, "https://api.example.com/users/123")
            .Respond("application/json", JsonSerializer.Serialize(updatedUser));

        var client = ApiClient.Create<ITestApi>(_httpClient, _options);

        // Act
        var result = await client.PatchUser(123, updatedUser);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task Request_WithNonSuccessStatusCode_ThrowsApiException()
    {
        // Arrange
        _mockHttp
            .When("https://api.example.com/users/999")
            .Respond(HttpStatusCode.NotFound, "application/json", "{\"error\":\"User not found\"}");

        var client = ApiClient.Create<ITestApi>(_httpClient, _options);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await client.GetUser(999));

        exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Content.Should().Contain("User not found");
    }

    [Fact]
    public void ValidateInterface_WithValidInterface_DoesNotThrow()
    {
        ApiClient.ValidateInterface<ITestApi>();
    }

    [Fact]
    public void ValidateInterface_WithInvalidReturnType_ThrowsApiConfigurationException()
    {
        Assert.Throws<ApiConfigurationException>(() =>
            ApiClient.ValidateInterface<IInvalidApi>());
    }
}

// Test interfaces
public interface ITestApi
{
    [Get("/users/{id}")]
    Task<User> GetUser(int id);

    [Post("/users")]
    Task<User> CreateUser([Body] User user);

    [Put("/users/{id}")]
    Task<User> UpdateUser(int id, [Body] User user);

    [Patch("/users/{id}")]
    Task<User> PatchUser(int id, [Body] User user);

    [Delete("/users/{id}")]
    Task DeleteUser(int id);

    [Get("/users")]
    Task<List<User>> GetUsers([Query] int page, [Query] int pageSize);

    [Get("/users/{id}")]
    Task<User> GetUserWithCancellation(int id, CancellationToken cancellationToken);

    [Get("/users/{id}")]
    Task<ApiResponse<User>> GetUserWithHeaders(int id);
}

public interface IInvalidApi
{
    [Get("/users/{id}")]
    User GetUser(int id); // Invalid: not returning Task
}

// Test models
public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
}

public class TestClass
{
    // Not an interface
}
