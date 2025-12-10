using CoreApiClient.Extensions;
using CoreApiClient.Models;
using FluentAssertions;
using RichardSzalay.MockHttp;
using Xunit;
using System.Net;

namespace CoreApiClient.Tests;

public class DownloadTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    private readonly string _testDirectory;

    public DownloadTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CoreApiClient_Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task DownloadFileAsync_WithProgress_ReportsProgress()
    {
        // Arrange
        var testData = new byte[1024]; // 1KB
        new Random().NextBytes(testData);

        _mockHttp
            .When("https://api.example.com/files/test.bin").WithHeaders("application/octet-stream")
            .Respond(new ByteArrayContent(testData));

        var progressReports = new List<DownloadProgress>();
        var progress = new Progress<DownloadProgress>(p => progressReports.Add(p));

        // Act
        var result = await _httpClient.DownloadFileAsync(
            "https://api.example.com/files/test.bin",
            progress);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().NotBeNull();
        result.Content!.Length.Should().Be(1024);
        result.BytesDownloaded.Should().Be(1024);
        progressReports.Should().NotBeEmpty();
        progressReports.Last().BytesDownloaded.Should().Be(1024);
    }

    [Fact]
    public async Task DownloadToFileAsync_SavesContentToFile()
    {
        // Arrange
        var testData = new byte[2048]; // 2KB
        new Random().NextBytes(testData);
        var filePath = Path.Combine(_testDirectory, "downloaded.bin");

        _mockHttp
            .When("https://api.example.com/files/test.bin").WithHeaders("application/octet-stream")
            .Respond(new ByteArrayContent(testData));

        // Act
        var result = await _httpClient.DownloadToFileAsync(
            "https://api.example.com/files/test.bin",
            filePath);

        // Assert
        result.Should().NotBeNull();
        result.IsSavedToFile.Should().BeTrue();
        result.FilePath.Should().Be(filePath);
        result.BytesDownloaded.Should().Be(2048);
        File.Exists(filePath).Should().BeTrue();
        File.ReadAllBytes(filePath).Should().BeEquivalentTo(testData);
    }

    [Fact]
    public async Task DownloadToFileAsync_WithProgress_ReportsProgress()
    {
        // Arrange
        var testData = new byte[4096]; // 4KB
        new Random().NextBytes(testData);
        var filePath = Path.Combine(_testDirectory, "progress.bin");

        _mockHttp
            .When("https://api.example.com/files/test.bin").WithHeaders("application/octet-stream")
            .Respond(new ByteArrayContent(testData));

        var progressReports = new List<DownloadProgress>();
        var progress = new Progress<DownloadProgress>(p => progressReports.Add(p));

        // Act
        var result = await _httpClient.DownloadToFileAsync(
            "https://api.example.com/files/test.bin",
            filePath,
            progress);

        // Assert
        result.BytesDownloaded.Should().Be(4096);
        progressReports.Should().NotBeEmpty();
        progressReports.Last().BytesDownloaded.Should().Be(4096);
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task DownloadRangeAsync_DownloadsPartialContent()
    {
        // Arrange
        var fullData = new byte[1000];
        for (int i = 0; i < fullData.Length; i++)
        {
            fullData[i] = (byte)(i % 256);
        }

        var rangeData = fullData[100..201]; // Bytes 100-200

        _mockHttp
            .When("https://api.example.com/files/test.bin")
            .WithHeaders("Range", "bytes=100-200").WithHeaders("application/octet-stream").WithContent("ResetContent")
            .Respond(new ByteArrayContent(rangeData));

        // Act
        var result = await _httpClient.DownloadRangeAsync(
            "https://api.example.com/files/test.bin",
            100,
            200);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().NotBeNull();
        result.Content!.Length.Should().Be(101); // 100-200 inclusive
        result.BytesDownloaded.Should().Be(101);
    }

    [Fact]
    public async Task GetFileSizeAsync_ReturnsContentLength()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Head, "https://api.example.com/files/test.bin")
            .Respond(request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(Array.Empty<byte>());
                response.Content.Headers.ContentLength = 5000;
                return Task.FromResult(response);
            });

        // Act
        var size = await _httpClient.GetFileSizeAsync("https://api.example.com/files/test.bin");

        // Assert
        size.Should().Be(5000);
    }

    [Fact]
    public async Task SupportsRangeDownloadsAsync_DetectsRangeSupport()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Head, "https://api.example.com/files/test.bin")
            .Respond(request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Headers.Add("Accept-Ranges", "bytes");
                return Task.FromResult(response);
            });

        // Act
        var supportsRanges = await _httpClient.SupportsRangeDownloadsAsync(
            "https://api.example.com/files/test.bin");

        // Assert
        supportsRanges.Should().BeTrue();
    }

    [Fact]
    public void DownloadProgress_CalculatesPercentageCorrectly()
    {
        // Arrange & Act
        var progress = new DownloadProgress
        {
            TotalBytes = 1000,
            BytesDownloaded = 250
        };

        // Assert
        progress.ProgressPercentage.Should().Be(25.0);
        progress.IsTotalSizeKnown.Should().BeTrue();
    }

    [Fact]
    public void DownloadProgress_FormatsBytes_Correctly()
    {
        // Arrange & Act
        var progress1 = new DownloadProgress { BytesDownloaded = 500 };
        var progress2 = new DownloadProgress { BytesDownloaded = 1500 };
        var progress3 = new DownloadProgress { BytesDownloaded = 1_500_000 };
        var progress4 = new DownloadProgress { BytesDownloaded = 1_500_000_000 };

        // Assert
        progress1.BytesDownloadedFormatted.Should().Be("500 B");
        progress2.BytesDownloadedFormatted.Should().Be("1.46 KB");
        progress3.BytesDownloadedFormatted.Should().Be("1.43 MB");
        progress4.BytesDownloadedFormatted.Should().Be("1.4 GB");
    }

    [Fact]
    public void DownloadResult_FormatsSize_Correctly()
    {
        // Arrange & Act
        var result = new DownloadResult
        {
            BytesDownloaded = 2_500_000
        };

        // Assert
        result.SizeFormatted.Should().Be("2.38 MB");
    }

    [Fact]
    public async Task DownloadFileAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var testData = new byte[1024];
        _mockHttp
            .When("https://api.example.com/files/test.bin").WithHeaders("application/octet-stream")
            .Respond(new ByteArrayContent(testData));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _httpClient.DownloadFileAsync(
                "https://api.example.com/files/test.bin",
                null,
                cts.Token));
    }

    [Fact]
    public async Task DownloadResult_ExtractsFileName_FromContentDisposition()
    {
        // Arrange
        var testData = new byte[100];

        _mockHttp
            .When("https://api.example.com/files/test.bin")
            .Respond(request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(testData)
                };
                response.Content.Headers.Add("Content-Disposition", "attachment; filename=\"myfile.pdf\"");
                return Task.FromResult(response);
            });

        // Act
        var result = await _httpClient.DownloadFileAsync("https://api.example.com/files/test.bin");

        // Assert
        result.SuggestedFileName.Should().Be("myfile.pdf");
    }

    [Fact]
    public async Task DownloadToFileAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var testData = new byte[512];
        new Random().NextBytes(testData);
        var subDir = Path.Combine(_testDirectory, "subdir", "nested");
        var filePath = Path.Combine(subDir, "file.bin");

        _mockHttp
            .When("https://api.example.com/files/test.bin").WithHeaders("application/octet-stream")
            .Respond(new ByteArrayContent(testData));

        // Act
        var result = await _httpClient.DownloadToFileAsync(
            "https://api.example.com/files/test.bin",
            filePath);

        // Assert
        Directory.Exists(subDir).Should().BeTrue();
        File.Exists(filePath).Should().BeTrue();
        result.BytesDownloaded.Should().Be(512);
    }

    [Fact]
    public void DownloadProgress_WithUnknownSize_ReturnsNullPercentage()
    {
        // Arrange & Act
        var progress = new DownloadProgress
        {
            TotalBytes = null,
            BytesDownloaded = 500
        };

        // Assert
        progress.ProgressPercentage.Should().BeNull();
        progress.IsTotalSizeKnown.Should().BeFalse();
        progress.BytesDownloadedFormatted.Should().Be("500 B");
        progress.TotalBytesFormatted.Should().BeNull();
    }

    [Fact]
    public void RangeDownloadOptions_GetRangeHeader_FormatsCorrectly()
    {
        // Arrange & Act
        var options1 = new RangeDownloadOptions { Start = 100, End = 200 };
        var options2 = new RangeDownloadOptions { Start = 500, End = null };

        // Assert
        options1.GetRangeHeader().Should().Be("bytes=100-200");
        options2.GetRangeHeader().Should().Be("bytes=500-");
    }
}