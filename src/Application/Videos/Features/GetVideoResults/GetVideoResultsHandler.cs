using Application.Videos.Ports;
using Domain.Common;
using MediatR;

namespace Application.Videos.Features.GetVideoResults;

public class GetVideoResultsHandler(IVideoProcessingRepository repository) 
    : IRequestHandler<GetVideoResultsQuery, Result<GetVideoResultsResult>>
{
    public async Task<Result<GetVideoResultsResult>> Handle(GetVideoResultsQuery query, CancellationToken cancellationToken)
    {
        var videoProcessingResult = await repository.GetByVideoIdAsync(query.VideoId, cancellationToken);
        if (!videoProcessingResult.IsSuccess)
            return Result<GetVideoResultsResult>.FromResult(videoProcessingResult);
        var videoProcessing = videoProcessingResult.Value!;

        return new GetVideoResultsResult(
            videoProcessing.VideoId,
            videoProcessing.Status,
            videoProcessing.CompletedAt,
            videoProcessing.TotalQrCodes,
            videoProcessing.QrCodes);
    }
}