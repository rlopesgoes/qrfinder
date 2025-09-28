using Domain.Models;

namespace Application.UseCases.GetAnalysisResults;

public record GetAnalysisResultsResult(
    string VideoId,
    string Status,
    DateTime? CompletedAt,
    int TotalQrCodes,
    QrCodes QrCodes);