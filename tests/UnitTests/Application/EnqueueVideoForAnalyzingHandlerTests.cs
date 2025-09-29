using Application.Ports;
using Application.UseCases.EnqueueVideoForAnalyzing;
using Domain.Common;
using Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Application;

public class EnqueueVideoForAnalyzingHandlerTests
{
    private readonly Mock<IStatusReadOnlyRepository> _statusReadOnlyRepository;
    private readonly Mock<IStatusWriteOnlyRepository> _statusWriteOnlyRepository;
    private readonly Mock<IVideoAnalysisQueue> _videoAnalysisQueue;
    private readonly Mock<IProgressNotifier> _progressNotifier;
    private readonly EnqueueVideoForAnalyzingHandler _handler;

    public EnqueueVideoForAnalyzingHandlerTests()
    {
        _statusReadOnlyRepository = new Mock<IStatusReadOnlyRepository>();
        _statusWriteOnlyRepository = new Mock<IStatusWriteOnlyRepository>();
        _videoAnalysisQueue = new Mock<IVideoAnalysisQueue>();
        _progressNotifier = new Mock<IProgressNotifier>();
        
        _handler = new EnqueueVideoForAnalyzingHandler(
            _statusReadOnlyRepository.Object,
            _statusWriteOnlyRepository.Object,
            _videoAnalysisQueue.Object,
            _progressNotifier.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Success_When_Video_Is_Created_Status()
    {
        // Arrange
        var videoId = "test-video-id";
        var command = new EnqueueVideoForAnalyzingCommand(videoId);
        var status = new Status(videoId, Stage.Created);
        
        _statusReadOnlyRepository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Status>.Success(status));
            
        _videoAnalysisQueue
            .Setup(x => x.EnqueueAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
            
        _statusWriteOnlyRepository
            .Setup(x => x.UpsertAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
            
        _progressNotifier
            .Setup(x => x.NotifyAsync(It.IsAny<ProgressNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.VideoId.Should().Be(videoId);
        result.Value.EnqueuedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Status_Repository_Fails()
    {
        // Arrange
        var videoId = "test-video-id";
        var command = new EnqueueVideoForAnalyzingCommand(videoId);
        var errorMessage = "Database error";
        
        _statusReadOnlyRepository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Status>.WithError(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Video_Is_Already_Being_Processed()
    {
        // Arrange
        var videoId = "test-video-id";
        var command = new EnqueueVideoForAnalyzingCommand(videoId);
        var status = new Status(videoId, Stage.Processing);
        
        _statusReadOnlyRepository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Status>.Success(status));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Contain("already being processed");
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Queue_Enqueue_Fails()
    {
        // Arrange
        var videoId = "test-video-id";
        var command = new EnqueueVideoForAnalyzingCommand(videoId);
        var status = new Status(videoId, Stage.Created);
        var queueError = "Queue connection failed";
        
        _statusReadOnlyRepository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Status>.Success(status));
            
        _videoAnalysisQueue
            .Setup(x => x.EnqueueAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.WithError(queueError));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be(queueError);
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Status_Update_Fails()
    {
        // Arrange
        var videoId = "test-video-id";
        var command = new EnqueueVideoForAnalyzingCommand(videoId);
        var status = new Status(videoId, Stage.Created);
        var updateError = "Update failed";
        
        _statusReadOnlyRepository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Status>.Success(status));
            
        _videoAnalysisQueue
            .Setup(x => x.EnqueueAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
            
        _statusWriteOnlyRepository
            .Setup(x => x.UpsertAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.WithError(updateError));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be(updateError);
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Progress_Notification_Fails()
    {
        // Arrange
        var videoId = "test-video-id";
        var command = new EnqueueVideoForAnalyzingCommand(videoId);
        var status = new Status(videoId, Stage.Created);
        var notificationError = "Notification failed";
        
        _statusReadOnlyRepository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Status>.Success(status));
            
        _videoAnalysisQueue
            .Setup(x => x.EnqueueAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
            
        _statusWriteOnlyRepository
            .Setup(x => x.UpsertAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
            
        _progressNotifier
            .Setup(x => x.NotifyAsync(It.IsAny<ProgressNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.WithError(notificationError));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be(notificationError);
    }

    [Fact]
    public async Task Handle_Should_Call_All_Dependencies_In_Correct_Order()
    {
        // Arrange
        var videoId = "test-video-id";
        var command = new EnqueueVideoForAnalyzingCommand(videoId);
        var status = new Status(videoId, Stage.Created);
        
        _statusReadOnlyRepository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Status>.Success(status));
            
        _videoAnalysisQueue
            .Setup(x => x.EnqueueAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
            
        _statusWriteOnlyRepository
            .Setup(x => x.UpsertAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
            
        _progressNotifier
            .Setup(x => x.NotifyAsync(It.IsAny<ProgressNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _statusReadOnlyRepository.Verify(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()), Times.Once);
        _videoAnalysisQueue.Verify(x => x.EnqueueAsync(videoId, It.IsAny<CancellationToken>()), Times.Once);
        _statusWriteOnlyRepository.Verify(x => x.UpsertAsync(It.Is<Status>(s => s.VideoId == videoId && s.Stage == Stage.Sent), It.IsAny<CancellationToken>()), Times.Once);
        _progressNotifier.Verify(x => x.NotifyAsync(It.Is<ProgressNotification>(p => p.VideoId == videoId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(Stage.Sent)]
    [InlineData(Stage.Processing)]
    [InlineData(Stage.Processed)]
    [InlineData(Stage.Failed)]
    public async Task Handle_Should_Return_Error_When_Video_Is_Not_In_Created_Stage(Stage currentStage)
    {
        // Arrange
        var videoId = "test-video-id";
        var command = new EnqueueVideoForAnalyzingCommand(videoId);
        var status = new Status(videoId, currentStage);
        
        _statusReadOnlyRepository
            .Setup(x => x.GetAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Status>.Success(status));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Contain("already being processed");
    }
}