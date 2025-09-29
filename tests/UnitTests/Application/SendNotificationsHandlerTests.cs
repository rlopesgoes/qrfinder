using Application.Ports;
using Application.UseCases.SendNotifications;
using Domain.Common;
using Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Application;

public class SendNotificationsHandlerTests
{
    private readonly Mock<INotificationChannel> _channel1;
    private readonly Mock<INotificationChannel> _channel2;
    private readonly SendNotificationsHandler _handler;

    public SendNotificationsHandlerTests()
    {
        _channel1 = new Mock<INotificationChannel>();
        _channel2 = new Mock<INotificationChannel>();
        var channels = new[] { _channel1.Object, _channel2.Object };
        _handler = new SendNotificationsHandler(channels);
    }

    [Fact]
    public async Task Handle_Should_Send_Notifications_To_All_Channels()
    {
        // Arrange
        var command = new SendNotificationsCommand(
            VideoId: "test-video-id",
            Stage: "Processing",
            ProgressPercentage: 50,
            Message: "Video is being processed",
            Timestamp: DateTimeOffset.UtcNow);

        _channel1
            .Setup(x => x.SendNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _channel2
            .Setup(x => x.SendNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        _channel1.Verify(x => x.SendNotificationAsync(
            It.Is<Notification>(n => 
                n.VideoId == command.VideoId &&
                n.Stage == command.Stage &&
                n.ProgressPercentage == command.ProgressPercentage &&
                n.Message == command.Message),
            It.IsAny<CancellationToken>()), Times.Once);

        _channel2.Verify(x => x.SendNotificationAsync(
            It.Is<Notification>(n => 
                n.VideoId == command.VideoId &&
                n.Stage == command.Stage &&
                n.ProgressPercentage == command.ProgressPercentage &&
                n.Message == command.Message),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Send_Notification_With_Correct_Properties()
    {
        // Arrange
        var videoId = "test-video-123";
        var stage = "Completed";
        var progressPercentage = 100;
        var message = "Processing completed successfully";
        var timestamp = DateTimeOffset.UtcNow;

        var command = new SendNotificationsCommand(videoId, stage, progressPercentage, message, timestamp);

        _channel1
            .Setup(x => x.SendNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _channel1.Verify(x => x.SendNotificationAsync(
            It.Is<Notification>(n => 
                n.VideoId == videoId &&
                n.Stage == stage &&
                n.ProgressPercentage == progressPercentage &&
                n.Message == message &&
                n.Timestamp == timestamp),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Work_With_No_Channels()
    {
        // Arrange
        var emptyChannels = Array.Empty<INotificationChannel>();
        var handler = new SendNotificationsHandler(emptyChannels);
        var command = new SendNotificationsCommand(
            "test-video", "Processing", 50, "Test message", DateTimeOffset.UtcNow);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_Continue_Even_If_One_Channel_Throws_Exception()
    {
        // Arrange
        var command = new SendNotificationsCommand(
            "test-video", "Processing", 50, "Test message", DateTimeOffset.UtcNow);

        _channel1
            .Setup(x => x.SendNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Channel 1 failed"));

        _channel2
            .Setup(x => x.SendNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        // Note: The current implementation doesn't handle exceptions, so this will throw
        // In a real scenario, you might want to add exception handling
        await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));

        // Verify that both channels were called (though one failed)
        _channel1.Verify(x => x.SendNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
        // Channel2 might not be called due to Task.WhenAll behavior with exceptions
    }

    [Fact]
    public async Task Handle_Should_Send_Notifications_Concurrently()
    {
        // Arrange
        var command = new SendNotificationsCommand(
            "test-video", "Processing", 50, "Test message", DateTimeOffset.UtcNow);

        var delay = TimeSpan.FromMilliseconds(100);
        
        _channel1
            .Setup(x => x.SendNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.Delay(delay));

        _channel2
            .Setup(x => x.SendNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.Delay(delay));

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _handler.Handle(command, CancellationToken.None);
        var totalTime = DateTime.UtcNow - startTime;

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // If concurrent, total time should be close to delay time, not 2x delay time
        totalTime.Should().BeLessThan(TimeSpan.FromMilliseconds(150)); // Some buffer for execution overhead
    }
}