namespace Application.Videos.Features.GetVideoResults;

public record QrCodeResultDto(
    string Text,
    double TimestampSeconds,
    string FormattedTime,
    DateTime DetectedAt);