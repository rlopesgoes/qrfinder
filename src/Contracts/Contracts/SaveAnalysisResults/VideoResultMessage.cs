namespace Contracts.Contracts.SaveAnalysisResults;

public record VideoResultMessage(
    string VideoId,
    DateTimeOffset CompletedAt,
    double ProcessingTimeMs,
    QrCodeDto[] QrCodes);
    
public record QrCodeDto(
    string Text,
    double TimestampSeconds,
    string FormattedTimestamp);