using Application.Videos.Ports;
using Domain.Videos;
using MediatR;

namespace Application.Videos.UseCases.StoreVideoChunk;

public record StoreVideoChunkCommand(string VideoId, byte[] ChunkData) : IRequest;

public class StoreVideoChunkHandler(IVideoChunkStorage chunkStorage) 
    : IRequestHandler<StoreVideoChunkCommand>
{
    public async Task Handle(StoreVideoChunkCommand request, CancellationToken cancellationToken)
    {
        var videoId = VideoId.From(request.VideoId);
        
        // Store chunk (preserving exact original logic - stateless file storage)
        await chunkStorage.StoreChunkAsync(videoId, request.ChunkData, cancellationToken);
    }
}