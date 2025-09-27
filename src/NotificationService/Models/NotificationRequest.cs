namespace NotificationService.Models;

public record NotificationRequest(
    string VideoId,
    VideoProcessingStage Stage,
    double ProgressPercentage,
    string? Message = null,
    DateTime Timestamp = default);

public enum VideoProcessingStage
{
    Sent,
}