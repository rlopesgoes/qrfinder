namespace Application.Videos.GetVideoResults;

public record GetVideoResultsResponse(
    string VideoId,
    string Status,
    DateTime? CompletedAt,
    int TotalQRCodes,
    QRCodeResultDto[] QRCodes);

public record QRCodeResultDto(
    string Text,
    double TimestampSeconds,
    string FormattedTime,
    DateTime DetectedAt);