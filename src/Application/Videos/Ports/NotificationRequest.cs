using Domain.Videos;

namespace Application.Videos.Ports;

public record NotificationRequest(
    string VideoId,
    VideoProcessingStage Stage,
    double ProgressPercentage,
    string? Message = null,
    DateTime Timestamp = default);

