using Domain.Common;
using Domain.Models;
using MediatR;

namespace Application.UseCases.SaveAnalysisResults;

public record SaveAnalysisResultsCommand(
    string VideoId,
    DateTimeOffset CompletedAt,
    double ProcessingTimeMs,
    QrCodes QrCodes) 
    : IRequest<Result<SaveAnalysisResultsResult>>;
    