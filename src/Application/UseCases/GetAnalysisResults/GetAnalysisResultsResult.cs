namespace Application.Videos.Features.GetVideoResults;

public record GetAnalysisResultsResult(
    string VideoId,
    string Status,
    DateTime? CompletedAt,
    int TotalQrCodes,
    IReadOnlyCollection<QrCodeResult> QrCodes);
    
public record QrCodeResult(
    string Text,
    double TimestampSeconds,
    string FormattedTimestamp,
    DateTime DetectedAt);