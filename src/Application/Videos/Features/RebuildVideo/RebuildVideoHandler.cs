using Application.Videos.Ports;
using MediatR;

namespace Application.Videos.Features.RebuildVideo;

public class StoreVideoPartHandler(IVideoStorageService videoStorageService) 
    : IRequestHandler<RebuildVideoRequest>
{
    public Task Handle(RebuildVideoRequest request, CancellationToken cancellationToken)
        => videoStorageService.StoreVideoPartAsync(request.VideoId, request.VideoPart, cancellationToken);
}