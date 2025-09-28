using Application.Ports;
using Domain.Common;
using Domain.Models;
using MediatR;

namespace Application.UseCases.SaveAnalysisResults;

public class SaveAnalysisResultsHandler(IAnalysisResultWriteOnlyRepository analysisResultWriteOnlyRepository) : IRequestHandler<SaveAnalysisResultsCommand, Result<SaveAnalysisResultsResult>>
{
    public async Task<Result<SaveAnalysisResultsResult>> Handle(
        SaveAnalysisResultsCommand request, CancellationToken cancellationToken)
    {
        var videoId = request.VideoId;

        var qrCodes = request.QrCodes.Values
            .Where(c => !string.IsNullOrEmpty(c.Content))
            .Select(c => new QrCode(
                c.Content!,
                c.TimeStamp))
            .ToList();

        var videoProcessingResult = new AnalysisResult(
            videoId,
            "Completed",
            request.CompletedAt.AddSeconds(-request.ProcessingTimeMs / 1000).DateTime,
            request.CompletedAt.DateTime,
            qrCodes.Count,
            new(qrCodes));

        var result = await analysisResultWriteOnlyRepository.SaveAsync(videoProcessingResult, cancellationToken);
        if (!result.IsSuccess)
            return Result<SaveAnalysisResultsResult>.FromResult(result);

        return new SaveAnalysisResultsResult();
    }
}