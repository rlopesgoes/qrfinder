using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace IntegrationTests;

public class WebApiIntegrationTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WebApiIntegrationTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GenerateUploadLink_Should_Return_Success()
    {
        // Act
        var response = await _client.PostAsync("/video/upload-link/generate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        // Debug: print the actual response content
        System.Console.WriteLine($"Response content: {content}");
        
        var result = JsonSerializer.Deserialize<GenerateUploadLinkResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        result.Should().NotBeNull();
        result!.VideoId.Should().NotBeNullOrEmpty();
        result.Url.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task GenerateUploadLink_With_VideoId_Should_Use_Provided_Id()
    {
        // Arrange
        var videoId = Guid.NewGuid();
        var request = new { VideoId = videoId };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/video/upload-link/generate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GenerateUploadLinkResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        result.Should().NotBeNull();
        result!.VideoId.Should().Be(videoId.ToString());
    }

    [Fact]
    public async Task EnqueueVideo_Should_Return_Success_When_Video_Exists()
    {
        // Arrange - First generate upload link
        var uploadResponse = await _client.PostAsync("/video/upload-link/generate", null);
        var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
        var uploadResult = JsonSerializer.Deserialize<GenerateUploadLinkResponse>(uploadContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Act - Enqueue the video
        var enqueueResponse = await _client.PatchAsync($"/video/{uploadResult!.VideoId}/analyze", null);

        // Assert
        enqueueResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var enqueueContent = await enqueueResponse.Content.ReadAsStringAsync();
        var enqueueResult = JsonSerializer.Deserialize<EnqueueVideoResponse>(enqueueContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        enqueueResult.Should().NotBeNull();
        enqueueResult!.VideoId.Should().Be(uploadResult.VideoId);
        enqueueResult.EnqueuedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetAnalysisStatus_Should_Return_Status_When_Video_Exists()
    {
        // Arrange - Create video
        var uploadResponse = await _client.PostAsync("/video/upload-link/generate", null);
        var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
        var uploadResult = JsonSerializer.Deserialize<GenerateUploadLinkResponse>(uploadContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Act
        var statusResponse = await _client.GetAsync($"/video/{uploadResult!.VideoId}/status");

        // Assert
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var statusContent = await statusResponse.Content.ReadAsStringAsync();
        var statusResult = JsonSerializer.Deserialize<GetAnalysisStatusResponse>(statusContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        statusResult.Should().NotBeNull();
        statusResult!.Status.Should().Be("Created");
        statusResult.LastUpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task API_Should_Be_Available()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Invalid_Endpoint_Should_Return_NotFound()
    {
        // Act
        var response = await _client.GetAsync("/invalid-endpoint");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

// Response DTOs for testing
public record GenerateUploadLinkResponse(string VideoId, string Url, DateTimeOffset ExpiresAt);
public record EnqueueVideoResponse(string VideoId, DateTimeOffset EnqueuedAt);
public record GetAnalysisStatusResponse(string Status, DateTimeOffset LastUpdatedAt);
public record GetAnalysisResultsResponse(string VideoId, QrCodeResult[] QrCodes);
public record QrCodeResult(string Content, string FormattedTimestamp);