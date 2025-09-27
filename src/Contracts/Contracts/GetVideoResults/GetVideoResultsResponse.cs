namespace Contracts.Contracts.GetVideoResults;

public record GetVideoResultsResponse(string VideoId,
    string Status,
    DateTime? CompletedAt,
    int TotalQrCodes,
    IReadOnlyCollection<QrCodeResultDto> QrCodes);
    
public record QrCodeResultDto(
    string Text,
    double TimestampSeconds,
    string FormattedTimestamp,
    DateTime DetectedAt);