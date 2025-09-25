namespace NotificationService.Models;

public record NotificationRequest(
    string VideoId,
    VideoProcessingStage Stage,
    double ProgressPercentage,
    string? CurrentOperation = null,
    string? ErrorMessage = null,
    DateTime Timestamp = default);

public enum VideoProcessingStage
{
    Uploading,
    Uploaded,
    Processing,
    Processed,
    Failed
}