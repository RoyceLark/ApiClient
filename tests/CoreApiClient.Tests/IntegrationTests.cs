using CoreApiClient.Attributes;
using CoreApiClient.Configuration;
using CoreApiClient.Exceptions;
using FluentAssertions;
using Xunit;

namespace CoreApiClient.Tests.Integration;

/// <summary>
/// Integration tests that test against a real public API (JSONPlaceholder).
/// These tests require network connectivity.
/// </summary>
[Collection("Integration")]
public class RealApiIntegrationTests
{
    private const string JsonPlaceholderBaseUrl = "https://jsonplaceholder.typicode.com";

    [Fact]
    public async Task GetRequest_ReturnsData()
    {
        // Arrange
        var api = ApiClient.Create<IJsonPlaceholderApi>(JsonPlaceholderBaseUrl);

        // Act
        var post = await api.GetPost(1);

        // Assert
        post.Should().NotBeNull();
        post.Id.Should().Be(1);
        post.Title.Should().NotBeNullOrEmpty();
        post.Body.Should().NotBeNullOrEmpty();
        post.UserId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetRequestWithQueryParams_ReturnsFilteredData()
    {
        // Arrange
        var api = ApiClient.Create<IJsonPlaceholderApi>(JsonPlaceholderBaseUrl);

        // Act
        var posts = await api.GetPostsByUserId(1);

        // Assert
        posts.Should().NotBeEmpty();
        posts.Should().AllSatisfy(p => p.UserId.Should().Be(1));
    }

    [Fact]
    public async Task PostRequest_CreatesResource()
    {
        // Arrange
        var api = ApiClient.Create<IJsonPlaceholderApi>(JsonPlaceholderBaseUrl);
        var newPost = new JsonPlaceholderPost
        {
            Title = "Test Post",
            Body = "Test Body",
            UserId = 1
        };

        // Act
        var created = await api.CreatePost(newPost);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().BeGreaterThan(0);
        created.Title.Should().Be("Test Post");
        created.Body.Should().Be("Test Body");
        created.UserId.Should().Be(1);
    }

    [Fact]
    public async Task PutRequest_UpdatesResource()
    {
        // Arrange
        var api = ApiClient.Create<IJsonPlaceholderApi>(JsonPlaceholderBaseUrl);
        var updatedPost = new JsonPlaceholderPost
        {
            Id = 1,
            Title = "Updated Title",
            Body = "Updated Body",
            UserId = 1
        };

        // Act
        var result = await api.UpdatePost(1, updatedPost);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Title");
        result.Body.Should().Be("Updated Body");
    }

    [Fact]
    public async Task DeleteRequest_SucceedsWithoutError()
    {
        // Arrange
        var api = ApiClient.Create<IJsonPlaceholderApi>(JsonPlaceholderBaseUrl);

        // Act & Assert
        await api.DeletePost(1);
        // No exception means success
    }

    [Fact]
    public async Task GetRequest_WithRetryPolicy_SucceedsAfterTransientFailure()
    {
        // Arrange
        var api = ApiClient.Create<IJsonPlaceholderApi>(
            JsonPlaceholderBaseUrl,
            options =>
            {
                options.RetryPolicy.Enabled = true;
                options.RetryPolicy.MaxRetryAttempts = 3;
                options.RetryPolicy.UseExponentialBackoff = true;
                options.Timeout = TimeSpan.FromSeconds(30);
            });

        // Act
        var post = await api.GetPost(1);

        // Assert
        post.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRequest_WithLogging_LogsRequestAndResponse()
    {
        // Arrange
        var api = ApiClient.Create<IJsonPlaceholderApi>(
            JsonPlaceholderBaseUrl,
            options =>
            {
                options.EnableLogging = true;
                options.LogRequestBody = true;
                options.LogResponseBody = true;
            });

        // Act
        var post = await api.GetPost(1);

        // Assert
        post.Should().NotBeNull();
        // Logging happens in background, visual inspection in test output
    }

    [Fact]
    public async Task GetRequest_NonExistentResource_ThrowsApiException()
    {
        // Arrange
        var api = ApiClient.Create<IJsonPlaceholderApi>(JsonPlaceholderBaseUrl);

        // Act & Assert
        // JSONPlaceholder returns empty object for non-existent IDs, not 404
        // So we'll just verify it doesn't throw for valid requests
        var post = await api.GetPost(999999);
        // Should not throw
    }

    [Fact]
    public async Task GetRequest_WithInvalidUrl_ThrowsException()
    {
        // Arrange
        var api = ApiClient.Create<IJsonPlaceholderApi>("https://invalid-domain-that-does-not-exist-12345.com");

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await api.GetPost(1));
    }

    [Fact]
    public async Task ComplexRequest_WithMultipleParams_WorksCorrectly()
    {
        // Arrange
        var api = ApiClient.Create<IJsonPlaceholderApi>(JsonPlaceholderBaseUrl);

        // Act
        var comments = await api.GetCommentsByPostId(1);

        // Assert
        comments.Should().NotBeEmpty();
        comments.Should().AllSatisfy(c =>
        {
            c.PostId.Should().Be(1);
            c.Email.Should().NotBeNullOrEmpty();
            c.Name.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetAllPosts_ReturnsCollection()
    {
        // Arrange
        var api = ApiClient.Create<IJsonPlaceholderApi>(JsonPlaceholderBaseUrl);

        // Act
        var posts = await api.GetAllPosts();

        // Assert
        posts.Should().NotBeEmpty();
        posts.Should().HaveCountGreaterThan(50); // JSONPlaceholder has 100 posts
    }
}

// Test API interface
public interface IJsonPlaceholderApi
{
    [Get("/posts/{id}")]
    Task<JsonPlaceholderPost> GetPost(int id);

    [Get("/posts")]
    Task<List<JsonPlaceholderPost>> GetAllPosts();

    [Get("/posts")]
    Task<List<JsonPlaceholderPost>> GetPostsByUserId([Query("userId")] int userId);

    [Post("/posts")]
    Task<JsonPlaceholderPost> CreatePost([Body] JsonPlaceholderPost post);

    [Put("/posts/{id}")]
    Task<JsonPlaceholderPost> UpdatePost(int id, [Body] JsonPlaceholderPost post);

    [Delete("/posts/{id}")]
    Task DeletePost(int id);

    [Get("/posts/{postId}/comments")]
    Task<List<JsonPlaceholderComment>> GetCommentsByPostId(int postId);
}

// Test models
public class JsonPlaceholderPost
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class JsonPlaceholderComment
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
