namespace Application;

public record VideoResultMessage(
    string? VideoId,
    DateTimeOffset CompletedAt,
    double ProcessingTimeMs,
    QrCodeMessageDto[]? QrCodes);

public record QrCodeMessageDto(
    string? Text,
    double TimestampSeconds,
    string? FormattedTimestamp);