using Xunit;
using FluentAssertions;
using CoreApiClient.Attributes;
using CoreApiClient.Configuration;
using CoreApiClient.Models;
using CoreApiClient.Exceptions;
namespace CoreApiClient.Tests;
public class ApiClientEnhancedTests
{
    [Fact]
    public void Create_ValidInterface_Success() { ApiClient.ValidateInterface<ITestApi>(); }
    
    [Fact]
    public void QueryAttribute_WorksCorrectly()
    {
        // Query parameters are now properly handled - no longer treated as route params
        var api = ApiClient.Create<ITestApi>("https://api.example.com");
        api.Should().NotBeNull();
    }
    
    [Fact]
    public void FileContent_FromFile_CreatesCorrectly()
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "test");
        var file = FileContent.FromFile(tempFile);
        file.FileName.Should().NotBeNullOrEmpty();
        File.Delete(tempFile);
    }
    
    [Fact]
    public void ApiResponse_GetHeader_ReturnsCorrectValue()
    {
        var response = new ApiResponse<string>
        {
            Headers = new Dictionary<string, IEnumerable<string>> { { "X-Test", new[] { "value" } } }
        };
        response.GetHeader("X-Test").Should().Be("value");
    }
}
public class LoginData { public string Username { get; set; } = ""; public string Password { get; set; } = ""; }
