using Domain.Videos;
using Domain.Videos.Ports;
using MediatR;

namespace Application.Videos.UseCases.StoreVideoChunk;

public record StoreVideoChunkCommand(string VideoId, byte[] ChunkData) : IRequest;

public class StoreVideoChunkHandler(IVideoContentService contentService) 
    : IRequestHandler<StoreVideoChunkCommand>
{
    public async Task Handle(StoreVideoChunkCommand request, CancellationToken cancellationToken)
    {
        var videoId = VideoId.From(request.VideoId);
        
        // Store chunk (preserving exact original logic - stateless file storage)
        await contentService.StoreChunkAsync(videoId, request.ChunkData, cancellationToken);
    }
}