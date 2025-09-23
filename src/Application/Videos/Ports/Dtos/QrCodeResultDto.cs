namespace Application.Videos.Ports.Dtos;

public record QrCodeResultDto(
    string Text,
    double TimestampSeconds,
    string FormattedTimestamp,
    DateTime DetectedAt);