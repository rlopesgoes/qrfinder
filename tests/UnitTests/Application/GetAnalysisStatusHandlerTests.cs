using Application.Ports;
using Application.UseCases.GetAnalysisStatus;
using Domain.Common;
using Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Application;

public class GetAnalysisStatusHandlerTests
{
    private readonly Mock<IStatusReadOnlyRepository> _statusReadOnlyRepository;
    private readonly GetAnalysisStatusHandler _handler;

    public GetAnalysisStatusHandlerTests()
    {
        _statusReadOnlyRepository = new Mock<IStatusReadOnlyRepository>();
        _handler = new GetAnalysisStatusHandler(_statusReadOnlyRepository.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Success_With_Status_Details()
    {
        // Arrange
        var videoId = "test-video-id";
        var query = new GetAnalysisStatusQuery(videoId);
        var updatedAt = DateTime.UtcNow;
        var status = new Status(videoId, Stage.Processing, updatedAt);
        
        _statusReadOnlyRepository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Status>.Success(status));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("Processing");
        result.Value.LastUpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Repository_Fails()
    {
        // Arrange
        var videoId = "test-video-id";
        var query = new GetAnalysisStatusQuery(videoId);
        var errorMessage = "Database connection failed";
        
        _statusReadOnlyRepository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Status>.WithError(errorMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Video_Not_Found()
    {
        // Arrange
        var videoId = "non-existent-video-id";
        var query = new GetAnalysisStatusQuery(videoId);
        
        _statusReadOnlyRepository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Status>.EntityNotFound("Video not found"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCode.NotFound);
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be("Video not found");
    }

    [Theory]
    [InlineData(Stage.Created, "Created")]
    [InlineData(Stage.Sent, "Sent")]
    [InlineData(Stage.Processing, "Processing")]
    [InlineData(Stage.Processed, "Processed")]
    [InlineData(Stage.Failed, "Failed")]
    public async Task Handle_Should_Return_Correct_Status_String_For_Each_Stage(Stage stage, string expectedStatus)
    {
        // Arrange
        var videoId = "test-video-id";
        var query = new GetAnalysisStatusQuery(videoId);
        var updatedAt = DateTime.UtcNow;
        var status = new Status(videoId, stage, updatedAt);
        
        _statusReadOnlyRepository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Status>.Success(status));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task Handle_Should_Call_Repository_With_Correct_VideoId()
    {
        // Arrange
        var videoId = "specific-video-id";
        var query = new GetAnalysisStatusQuery(videoId);
        var status = new Status(videoId, Stage.Created, DateTime.UtcNow);
        
        _statusReadOnlyRepository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Status>.Success(status));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _statusReadOnlyRepository.Verify(
            x => x.GetAsync(videoId, It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}