using Application.Videos.Ports;
using Domain.Common;
using MediatR;

namespace Application.Videos.Features.GetVideoResults;

public class GetAnalysisResultsHandler(IVideoProcessingRepository repository) 
    : IRequestHandler<GetAnalysisResultsQuery, Result<GetAnalysisResultsResult>>
{
    public async Task<Result<GetAnalysisResultsResult>> Handle(GetAnalysisResultsQuery query, CancellationToken cancellationToken)
    {
        var videoProcessingResult = await repository.GetByVideoIdAsync(query.VideoId, cancellationToken);
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