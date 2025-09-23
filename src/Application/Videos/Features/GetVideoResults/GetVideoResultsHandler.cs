using Application.Videos.Ports;
using MediatR;

namespace Application.Videos.Features.GetVideoResults;

public class GetVideoResultsHandler(IVideoProcessingRepository repository) 
    : IRequestHandler<GetVideoResultsRequest, GetVideoResultsResponse?>
{
    public async Task<GetVideoResultsResponse?> Handle(GetVideoResultsRequest request, CancellationToken cancellationToken)
    {
        var videoProcessing = await repository.GetByVideoIdAsync(request.VideoId, cancellationToken);
        
        if (videoProcessing == null)
            return null;

        return new GetVideoResultsResponse(
            videoProcessing.VideoId,
            videoProcessing.Status,
            videoProcessing.CompletedAt,
            videoProcessing.TotalQrCodes,
            videoProcessing.QrCodes);
    }
}