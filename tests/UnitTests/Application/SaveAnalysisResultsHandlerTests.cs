using Application.Ports;
using Application.UseCases.SaveAnalysisResults;
using Domain.Common;
using Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Application;

public class SaveAnalysisResultsHandlerTests
{
    private readonly Mock<IAnalysisResultWriteOnlyRepository> _repository;
    private readonly SaveAnalysisResultsHandler _handler;

    public SaveAnalysisResultsHandlerTests()
    {
        _repository = new Mock<IAnalysisResultWriteOnlyRepository>();
        _handler = new SaveAnalysisResultsHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_Should_Save_Analysis_Results_Successfully()
    {
        // Arrange
        var videoId = "test-video-id";
        var completedAt = DateTimeOffset.UtcNow;
        var processingTimeMs = 5000;
        var qrCodes = new QrCodes(new[]
        {
            new QrCode("https://example.com", new Timestamp(10.5)),
            new QrCode("https://test.com", new Timestamp(25.0))
        });

        var command = new SaveAnalysisResultsCommand(videoId, completedAt, processingTimeMs, qrCodes);

        _repository
            .Setup(x => x.SaveAsync(It.IsAny<AnalysisResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_Filter_Empty_QrCodes()
    {
        // Arrange
        var videoId = "test-video-id";
        var qrCodes = new QrCodes(new[]
        {
            new QrCode("https://example.com", new Timestamp(10.5)),
            new QrCode("", new Timestamp(15.0)), // Empty content
            new QrCode("https://test.com", new Timestamp(25.0))
        });

        var command = new SaveAnalysisResultsCommand(videoId, DateTimeOffset.UtcNow, 1000, qrCodes);

        _repository
            .Setup(x => x.SaveAsync(It.IsAny<AnalysisResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verify that only non-empty QR codes were saved
        _repository.Verify(x => x.SaveAsync(
            It.Is<AnalysisResult>(ar => ar.TotalQrCodes == 2), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Calculate_Correct_StartedAt_Time()
    {
        // Arrange
        var videoId = "test-video-id";
        var completedAt = DateTimeOffset.UtcNow;
        var processingTimeMs = 5000; // 5 seconds
        var expectedStartedAt = completedAt.AddSeconds(-5).DateTime;
        var qrCodes = new QrCodes(Array.Empty<QrCode>());

        var command = new SaveAnalysisResultsCommand(videoId, completedAt, processingTimeMs, qrCodes);

        _repository
            .Setup(x => x.SaveAsync(It.IsAny<AnalysisResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repository.Verify(x => x.SaveAsync(
            It.Is<AnalysisResult>(ar => 
                ar.StartedAt.Should().BeCloseTo(expectedStartedAt, TimeSpan.FromSeconds(1))), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Repository_Fails()
    {
        // Arrange
        var videoId = "test-video-id";
        var command = new SaveAnalysisResultsCommand(
            videoId, 
            DateTimeOffset.UtcNow, 
            1000, 
            new QrCodes(Array.Empty<QrCode>()));
        var errorMessage = "Database save failed";

        _repository
            .Setup(x => x.SaveAsync(It.IsAny<AnalysisResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.WithError(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Handle_Should_Create_AnalysisResult_With_Correct_Properties()
    {
        // Arrange
        var videoId = "test-video-id";
        var completedAt = DateTimeOffset.UtcNow;
        var processingTimeMs = 3000;
        var qrCodes = new QrCodes(new[]
        {
            new QrCode("https://example.com", new Timestamp(10.5))
        });

        var command = new SaveAnalysisResultsCommand(videoId, completedAt, processingTimeMs, qrCodes);

        _repository
            .Setup(x => x.SaveAsync(It.IsAny<AnalysisResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repository.Verify(x => x.SaveAsync(
            It.Is<AnalysisResult>(ar => 
                ar.VideoId == videoId &&
                ar.Status == "Completed" &&
                ar.CompletedAt == completedAt.DateTime &&
                ar.ProcessingTimeMs == processingTimeMs &&
                ar.TotalQrCodes == 1), 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}