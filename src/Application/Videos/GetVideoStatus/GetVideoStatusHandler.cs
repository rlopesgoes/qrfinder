using Application.Videos.Data;
using MediatR;

namespace Application.Videos.GetVideoStatus;

public class GetVideoStatusHandler(IVideoStatusRepository repository) 
    : IRequestHandler<GetVideoStatusRequest, GetVideoStatusResponse?>
{
    public async Task<GetVideoStatusResponse?> Handle(GetVideoStatusRequest request, CancellationToken cancellationToken)
    {
        var uploadStatus = await repository.GetAsync(request.VideoId, cancellationToken);
        
        if (uploadStatus == null)
            return null;

        return new GetVideoStatusResponse(
            uploadStatus.VideoId,
            uploadStatus.Stage.ToString());
    }
}