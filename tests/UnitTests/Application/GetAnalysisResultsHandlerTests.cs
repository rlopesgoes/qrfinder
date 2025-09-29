using Application.Ports;
using Application.UseCases.GetAnalysisResults;
using Domain.Common;
using Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Application;

public class GetAnalysisResultsHandlerTests
{
    private readonly Mock<IAnalysisResultReadOnlyRepository> _repository;
    private readonly GetAnalysisResultsHandler _handler;

    public GetAnalysisResultsHandlerTests()
    {
        _repository = new Mock<IAnalysisResultReadOnlyRepository>();
        _handler = new GetAnalysisResultsHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Success_With_Analysis_Results()
    {
        // Arrange
        var videoId = "test-video-id";
        var query = new GetAnalysisResultsQuery(videoId);
        var qrCodes = new QrCodes(new[]
        {
            new QrCode("https://example.com", new Timestamp(10.5)),
            new QrCode("https://test.com", new Timestamp(25.0))
        });
        var analysisResult = new AnalysisResult(
            videoId,
            "Completed",
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow,
            5000,
            2,
            qrCodes);

        _repository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AnalysisResult>.Success(analysisResult));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.VideoId.Should().Be(videoId);
        result.Value.Status.Should().Be("Completed");
        result.Value.TotalQrCodes.Should().Be(2);
        result.Value.QrCodes.Values.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Repository_Fails()
    {
        // Arrange
        var videoId = "test-video-id";
        var query = new GetAnalysisResultsQuery(videoId);
        var errorMessage = "Database error";

        _repository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AnalysisResult>.WithError(errorMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Video_Not_Found()
    {
        // Arrange
        var videoId = "non-existent-video";
        var query = new GetAnalysisResultsQuery(videoId);

        _repository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AnalysisResult>.EntityNotFound("Analysis result not found"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCode.NotFound);
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be("Analysis result not found");
    }

    [Fact]
    public async Task Handle_Should_Call_Repository_With_Correct_VideoId()
    {
        // Arrange
        var videoId = "specific-video-id";
        var query = new GetAnalysisResultsQuery(videoId);
        var analysisResult = new AnalysisResult(
            videoId,
            "Completed",
            DateTime.UtcNow,
            DateTime.UtcNow,
            1000,
            0,
            new QrCodes(Array.Empty<QrCode>()));

        _repository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AnalysisResult>.Success(analysisResult));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _repository.Verify(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()), Times.Once);
    }
}