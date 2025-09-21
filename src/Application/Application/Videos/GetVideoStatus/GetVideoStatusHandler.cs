using Application.Videos.Data;
using MediatR;

namespace Application.Videos.GetVideoStatus;

public class GetVideoStatusHandler(IVideoProcessingRepository repository) 
    : IRequestHandler<GetVideoStatusRequest, GetVideoStatusResponse?>
{
    public async Task<GetVideoStatusResponse?> Handle(GetVideoStatusRequest request, CancellationToken cancellationToken)
    {
        var videoProcessing = await repository.GetByVideoIdAsync(request.VideoId, cancellationToken);
        
        if (videoProcessing == null)
            return null;

        return new GetVideoStatusResponse(
            videoProcessing.VideoId,
            videoProcessing.Status.ToString(),
            videoProcessing.StartedAt,
            videoProcessing.CompletedAt,
            videoProcessing.TotalFramesProcessed,
            videoProcessing.QRCodes.Count,
            videoProcessing.ErrorMessage);
    }
}