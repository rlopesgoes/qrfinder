using Application.Videos.Data;
using MediatR;

namespace Application.Videos.GetVideoResults;

public class GetVideoResultsHandler(IVideoProcessingRepository repository) 
    : IRequestHandler<GetVideoResultsRequest, GetVideoResultsResponse?>
{
    public async Task<GetVideoResultsResponse?> Handle(GetVideoResultsRequest request, CancellationToken cancellationToken)
    {
        var videoProcessing = await repository.GetByVideoIdAsync(request.VideoId, cancellationToken);
        
        if (videoProcessing == null)
            return null;

        var qrCodes = videoProcessing.QRCodes.Select(qr => new QRCodeResultDto(
            qr.Text,
            qr.TimestampSeconds,
            qr.FormattedTime,
            qr.DetectedAt)).ToArray();

        return new GetVideoResultsResponse(
            videoProcessing.VideoId,
            videoProcessing.Status.ToString(),
            videoProcessing.CompletedAt,
            qrCodes.Length,
            qrCodes);
    }
}