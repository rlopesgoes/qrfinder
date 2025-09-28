namespace Domain.Models;

public record AnalysisResult(
    string VideoId,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TotalQrCodes,
    QrCodes QrCodes);