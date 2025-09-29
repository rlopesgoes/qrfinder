using Domain.Models;
using FluentAssertions;
using Xunit;

namespace UnitTests.Domain;

public class DomainModelsTests
{
    public class VideoIdTests
    {
        [Fact]
        public void New_Should_Generate_Valid_Guid()
        {
            // Act
            var videoId = VideoId.New();

            // Assert
            videoId.Value.Should().NotBe(Guid.Empty);
            videoId.ToString().Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void From_Should_Parse_Valid_Guid_String()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var guidString = guid.ToString();

            // Act
            var videoId = VideoId.From(guidString);

            // Assert
            videoId.Value.Should().Be(guid);
            videoId.ToString().Should().Be(guidString);
        }

        [Fact]
        public void From_Should_Throw_For_Invalid_Guid_String()
        {
            // Arrange
            var invalidGuid = "not-a-guid";

            // Act & Assert
            Assert.Throws<FormatException>(() => VideoId.From(invalidGuid));
        }

        [Fact]
        public void ToString_Should_Return_Guid_String()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var videoId = new VideoId(guid);

            // Act
            var result = videoId.ToString();

            // Assert
            result.Should().Be(guid.ToString());
        }
    }

    public class TimestampTests
    {
        [Fact]
        public void ToFormattedString_Should_Format_Seconds_Correctly()
        {
            // Arrange
            var timestamp = new Timestamp(125.456); // 2:05.456

            // Act
            var formatted = timestamp.ToFormattedString();

            // Assert
            formatted.Should().Be("02:05.456");
        }

        [Fact]
        public void ToFormattedString_Should_Handle_Less_Than_One_Minute()
        {
            // Arrange
            var timestamp = new Timestamp(45.123); // 0:45.123

            // Act
            var formatted = timestamp.ToFormattedString();

            // Assert
            formatted.Should().Be("00:45.123");
        }

        [Fact]
        public void ToDateTime_Should_Add_Seconds_To_Base_Time()
        {
            // Arrange
            var baseTime = new DateTime(2023, 1, 1, 12, 0, 0);
            var timestamp = new Timestamp(90.5); // 1 minute 30.5 seconds

            // Act
            var result = timestamp.ToDateTime(baseTime);

            // Assert
            result.Should().Be(new DateTime(2023, 1, 1, 12, 1, 30, 500));
        }
    }

    public class StatusTests
    {
        [Fact]
        public void Status_Should_Have_Correct_Properties()
        {
            // Arrange
            var videoId = "test-video-id";
            var stage = Stage.Processing;
            var updatedAt = DateTime.UtcNow;

            // Act
            var status = new Status(videoId, stage, updatedAt);

            // Assert
            status.VideoId.Should().Be(videoId);
            status.Stage.Should().Be(stage);
            status.UpdatedAtUtc.Should().Be(updatedAt);
        }

        [Fact]
        public void Status_Should_Allow_Null_UpdatedAt()
        {
            // Arrange
            var videoId = "test-video-id";
            var stage = Stage.Created;

            // Act
            var status = new Status(videoId, stage);

            // Assert
            status.VideoId.Should().Be(videoId);
            status.Stage.Should().Be(stage);
            status.UpdatedAtUtc.Should().BeNull();
        }
    }

    public class UploadLinkTests
    {
        [Fact]
        public void UploadLink_Should_Have_Correct_Properties()
        {
            // Arrange
            var url = "https://storage.example.com/upload?token=abc123";
            var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

            // Act
            var uploadLink = new UploadLink(url, expiresAt);

            // Assert
            uploadLink.Url.Should().Be(url);
            uploadLink.ExpiresAt.Should().Be(expiresAt);
        }
    }

    public class VideoTests
    {
        [Fact]
        public void Video_Should_Have_Correct_Properties()
        {
            // Arrange
            var id = "test-video-id";
            var content = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

            // Act
            var video = new Video(id, content);

            // Assert
            video.Id.Should().Be(id);
            video.Content.Should().BeSameAs(content);
        }
    }

    public class StageTests
    {
        [Fact]
        public void Stage_Should_Have_All_Expected_Values()
        {
            // Assert
            Enum.GetValues<Stage>().Should().BeEquivalentTo(new[]
            {
                Stage.Created,
                Stage.Sent,
                Stage.Processing,
                Stage.Processed,
                Stage.Failed
            });
        }

        [Theory]
        [InlineData(Stage.Created)]
        [InlineData(Stage.Sent)]
        [InlineData(Stage.Processing)]
        [InlineData(Stage.Processed)]
        [InlineData(Stage.Failed)]
        public void Stage_Values_Should_Be_Valid(Stage stage)
        {
            // Act & Assert
            stage.Should().BeDefined();
        }
    }

    public class AnalysisResultTests
    {
        [Fact]
        public void AnalysisResult_Should_Have_Correct_Properties()
        {
            // Arrange
            var videoId = "test-video-id";
            var status = "Completed";
            var startedAt = DateTime.UtcNow.AddMinutes(-5);
            var completedAt = DateTime.UtcNow;
            var processingTimeMs = 5000.0;
            var totalQrCodes = 2;
            var qrCodes = new QrCodes(new[]
            {
                new QrCode("https://example.com", new Timestamp(10.0)),
                new QrCode("https://test.com", new Timestamp(20.0))
            });

            // Act
            var result = new AnalysisResult(
                videoId, status, startedAt, completedAt, 
                processingTimeMs, totalQrCodes, qrCodes);

            // Assert
            result.VideoId.Should().Be(videoId);
            result.Status.Should().Be(status);
            result.StartedAt.Should().Be(startedAt);
            result.CompletedAt.Should().Be(completedAt);
            result.ProcessingTimeMs.Should().Be(processingTimeMs);
            result.TotalQrCodes.Should().Be(totalQrCodes);
            result.QrCodes.Should().Be(qrCodes);
        }
    }

    public class NotificationTests
    {
        [Fact]
        public void Notification_Should_Have_Correct_Properties()
        {
            // Arrange
            var videoId = "test-video-id";
            var stage = "Processing";
            var progressPercentage = 50;
            var message = "Video is being processed";
            var timestamp = DateTimeOffset.UtcNow;

            // Act
            var notification = new Notification(videoId, stage, progressPercentage, message, timestamp);

            // Assert
            notification.VideoId.Should().Be(videoId);
            notification.Stage.Should().Be(stage);
            notification.ProgressPercentage.Should().Be(progressPercentage);
            notification.Message.Should().Be(message);
            notification.Timestamp.Should().Be(timestamp);
        }
    }

    public class ProgressNotificationTests
    {
        [Fact]
        public void ProgressNotification_Should_Have_Correct_Properties()
        {
            // Arrange
            var videoId = "test-video-id";
            var stage = "Processing";
            var progressPercentage = 75;
            var message = "Processing video frames";

            // Act
            var notification = new ProgressNotification(videoId, stage, progressPercentage, message);

            // Assert
            notification.VideoId.Should().Be(videoId);
            notification.Stage.Should().Be(stage);
            notification.ProgressPercentage.Should().Be(progressPercentage);
            notification.Message.Should().Be(message);
        }

        [Fact]
        public void ProgressNotification_Should_Allow_Optional_Parameters()
        {
            // Arrange
            var videoId = "test-video-id";
            var stage = "Created";

            // Act
            var notification = new ProgressNotification(videoId, Stage: stage);

            // Assert
            notification.VideoId.Should().Be(videoId);
            notification.Stage.Should().Be(stage);
            notification.ProgressPercentage.Should().BeNull();
            notification.Message.Should().BeNull();
        }
    }
}