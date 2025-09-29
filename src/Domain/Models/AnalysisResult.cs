namespace Domain.Models;

public record AnalysisResult(
    string VideoId,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    double ProcessingTimeMs,
    int TotalQrCodes,
    QrCodes QrCodes);