using System.Reflection;
using System.Text.Json;
using CoreApiClient.Attributes;
using CoreApiClient.Internal;
using FluentAssertions;
using Xunit;

namespace CoreApiClient.Tests;

public class RequestBuilderTests
{
    private readonly JsonSerializerOptions _jsonOptions;

    public RequestBuilderTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public void BuildRequest_WithGetMethod_CreatesCorrectRequest()
    {
        // Arrange
        var method = typeof(ITestRequestApi).GetMethod(nameof(ITestRequestApi.GetItem))!;
        var args = new object[] { "123" };

        // Act
        var (request, timeout) = RequestBuilder.BuildRequest(method, args, "https://api.example.com", _jsonOptions, out var ct);

        // Assert
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be("https://api.example.com/items/123");
        request.Content.Should().BeNull();
        timeout.Should().BeNull();
    }

    [Fact]
    public void BuildRequest_WithPostMethod_CreatesCorrectRequest()
    {
        // Arrange
        var method = typeof(ITestRequestApi).GetMethod(nameof(ITestRequestApi.CreateItem))!;
        var item = new TestItem { Name = "Test Item" };
        var args = new object[] { item };

        // Act
        var (request, timeout) = RequestBuilder.BuildRequest(method, args, "https://api.example.com", _jsonOptions, out var ct);

        // Assert
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.Should().Be("https://api.example.com/items");
        request.Content.Should().NotBeNull();
        request.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public void BuildRequest_WithQueryParameters_AppendsQueryString()
    {
        // Arrange
        var method = typeof(ITestRequestApi).GetMethod(nameof(ITestRequestApi.SearchItems))!;
        var args = new object?[] { "test query", 10, 1 };

        // Act
        var (request, timeout) = RequestBuilder.BuildRequest(method, args, "https://api.example.com", _jsonOptions, out var ct);

        // Assert
        request.RequestUri!.AbsoluteUri.Should().Contain("query=test%20query");
        request.RequestUri.AbsoluteUri.Should().Contain("limit=10");
        request.RequestUri.AbsoluteUri.Should().Contain("offset=1");
    }

    [Fact]
    public void BuildRequest_WithHeaders_AddsHeaders()
    {
        // Arrange
        var method = typeof(ITestRequestApi).GetMethod(nameof(ITestRequestApi.GetItemWithAuth))!;
        var args = new object[] { "123", "Bearer token123" };

        // Act
        var (request, timeout) = RequestBuilder.BuildRequest(method, args, "https://api.example.com", _jsonOptions, out var ct);

        // Assert
        request.Headers.Should().ContainKey("Authorization");
        request.Headers.GetValues("Authorization").First().Should().Be("Bearer token123");
    }

    [Fact]
    public void BuildRequest_WithMultipleRouteParameters_ReplacesAll()
    {
        // Arrange
        var method = typeof(ITestRequestApi).GetMethod(nameof(ITestRequestApi.GetNestedItem))!;
        var args = new object[] { "parent1", "child1" };

        // Act
        var (request, timeout) = RequestBuilder.BuildRequest(method, args, "https://api.example.com", _jsonOptions, out var ct);

        // Assert
        request.RequestUri.Should().Be("https://api.example.com/parents/parent1/children/child1");
    }

    [Fact]
    public void BuildRequest_WithNullParameter_SkipsParameter()
    {
        // Arrange
        var method = typeof(ITestRequestApi).GetMethod(nameof(ITestRequestApi.SearchItems))!;
        var args = new object?[] { "test", null, null };

        // Act
        var (request, timeout) = RequestBuilder.BuildRequest(method, args, "https://api.example.com", _jsonOptions, out var ct);

        // Assert
        request.RequestUri!.Query.Should().Contain("query=test");
        request.RequestUri.Query.Should().NotContain("limit");
        request.RequestUri.Query.Should().NotContain("offset");
    }

    [Fact]
    public void BuildRequest_WithSpecialCharactersInRoute_EncodesCorrectly()
    {
        // Arrange
        var method = typeof(ITestRequestApi).GetMethod(nameof(ITestRequestApi.GetItem))!;
        var args = new object[] { "test@123" };

        // Act
        var (request, timeout) = RequestBuilder.BuildRequest(method, args, "https://api.example.com", _jsonOptions, out var ct);

        // Assert
        request.RequestUri!.AbsoluteUri.Should().Contain("test%40123");
    }

    [Fact]
    public void BuildRequest_WithCancellationToken_ExtractsCancellationToken()
    {
        // Arrange
        var method = typeof(ITestRequestApi).GetMethod(nameof(ITestRequestApi.GetItemWithCancellation))!;
        var cts = new CancellationTokenSource();
        var args = new object[] { "123", cts.Token };

        // Act
        var (request, timeout) = RequestBuilder.BuildRequest(method, args, "https://api.example.com", _jsonOptions, out var extractedCt);

        // Assert
        extractedCt.Should().Be(cts.Token);
    }

    [Fact]
    public void BuildRequest_WithPerRequestTimeout_ReturnsTimeout()
    {
        // Arrange
        var method = typeof(ITestRequestApi).GetMethod(nameof(ITestRequestApi.GetItemWithTimeout))!;
        var args = new object[] { "123" };

        // Act
        var (request, timeout) = RequestBuilder.BuildRequest(method, args, "https://api.example.com", _jsonOptions, out var ct);

        // Assert
        timeout.Should().NotBeNull();
        timeout!.Value.TotalSeconds.Should().Be(5);
    }

    [Fact]
    public void GetReturnType_WithTask_ReturnsVoid()
    {
        // Arrange
        var method = typeof(ITestRequestApi).GetMethod(nameof(ITestRequestApi.DeleteItem))!;

        // Act
        var returnType = RequestBuilder.GetReturnType(method);

        // Assert
        returnType.Should().Be(typeof(void));
    }

    [Fact]
    public void GetReturnType_WithTaskOfT_ReturnsT()
    {
        // Arrange
        var method = typeof(ITestRequestApi).GetMethod(nameof(ITestRequestApi.GetItem))!;

        // Act
        var returnType = RequestBuilder.GetReturnType(method);

        // Assert
        returnType.Should().Be(typeof(TestItem));
    }

    [Fact]
    public void BuildRequest_WithoutHttpMethodAttribute_ThrowsException()
    {
        // Arrange
        var method = typeof(IInvalidRequestApi).GetMethod(nameof(IInvalidRequestApi.GetItem))!;
        var args = new object[] { "123" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            RequestBuilder.BuildRequest(method, args, "https://api.example.com", _jsonOptions, out var ct));
    }

    [Fact]
    public void BuildRequest_WithMissingRouteParameter_ThrowsException()
    {
        // Arrange
        var method = typeof(ITestRequestApi).GetMethod(nameof(ITestRequestApi.GetItem))!;
        var args = new object[] { }; // Missing required parameter

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() =>
            RequestBuilder.BuildRequest(method, args, "https://api.example.com", _jsonOptions, out var ct));
    }

    [Fact]
    public void IsApiResponseType_WithApiResponse_ReturnsTrue()
    {
        // Act
        var result = RequestBuilder.IsApiResponseType(typeof(CoreApiClient.Models.ApiResponse<TestItem>));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsApiResponseType_WithRegularType_ReturnsFalse()
    {
        // Act
        var result = RequestBuilder.IsApiResponseType(typeof(TestItem));

        // Assert
        result.Should().BeFalse();
    }
}

// Test interfaces
public interface ITestRequestApi
{
    [Get("/items/{id}")]
    Task<TestItem> GetItem(string id);

    [Post("/items")]
    Task<TestItem> CreateItem([Body] TestItem item);

    [Delete("/items/{id}")]
    Task DeleteItem(string id);

    [Get("/items")]
    Task<List<TestItem>> SearchItems(
        [Query] string? query,
        [Query] int? limit,
        [Query] int? offset);

    [Get("/items/{id}")]
    Task<TestItem> GetItemWithAuth(string id, [Header("Authorization")] string auth);

    [Get("/parents/{parentId}/children/{childId}")]
    Task<TestItem> GetNestedItem(string parentId, string childId);

    [Get("/items/{id}")]
    Task<TestItem> GetItemWithCancellation(string id, CancellationToken cancellationToken);

    [Get("/items/{id}")]
    Task<TestItem> GetItemWithTimeout(string id);
}

public interface IInvalidRequestApi
{
    Task<TestItem> GetItem(string id); // Missing HTTP method attribute
}

public class TestItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}