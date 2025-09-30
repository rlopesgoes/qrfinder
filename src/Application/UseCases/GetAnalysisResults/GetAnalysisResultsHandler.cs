using Application.Ports;
using Domain.Common;
using MediatR;

namespace Application.UseCases.GetAnalysisResults;

public class GetAnalysisResultsHandler(IAnalysisResultReadOnlyRepository repository) 
    : IRequestHandler<GetAnalysisResultsQuery, Result<GetAnalysisResultsResult>>
{
    public async Task<Result<GetAnalysisResultsResult>> Handle(GetAnalysisResultsQuery query, CancellationToken cancellationToken)
    {
        var videoProcessingResult = await repository.GetAsync(query.VideoId, cancellationToken);
        if (!videoProcessingResult.IsSuccess)
            return Result<GetAnalysisResultsResult>.FromResult(videoProcessingResult);
        var videoProcessing = videoProcessingResult.Value!;

        return new GetAnalysisResultsResult(
            videoProcessing.VideoId,
            videoProcessing.Status,
            videoProcessing.CompletedAt,
            videoProcessing.TotalQrCodes,
            videoProcessing.QrCodes);
    }
}