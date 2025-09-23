using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using Domain.Videos;
using MediatR;

namespace Application.Videos.UseCases.StoreVideoChunk;

public record StoreVideoChunkCommand(string VideoId, byte[] ChunkData, int SequenceNumber, long TotalExpectedSize) : IRequest;

public class StoreVideoChunkHandler(
    IVideoChunkStorage chunkStorage,
    IVideoStatusRepository statusRepository) 
    : IRequestHandler<StoreVideoChunkCommand>
{
    public async Task Handle(StoreVideoChunkCommand request, CancellationToken cancellationToken)
    {
        var videoId = VideoId.From(request.VideoId);
        var currentStatus = await statusRepository.GetAsync(request.VideoId, cancellationToken);
        
        EnsureVideoCanReceiveChunks(currentStatus);

        await chunkStorage.StoreChunkAsync(videoId, request.ChunkData, cancellationToken);

        var newReceivedBytes = (currentStatus?.ReceivedBytes ?? 0) + request.ChunkData.Length;
        var isUploadComplete = newReceivedBytes >= request.TotalExpectedSize;
        
        var newStage = isUploadComplete ? UploadStage.Uploaded : UploadStage.Uploading;
        
        await statusRepository.UpsertAsync(
            new UploadStatus(
                request.VideoId, 
                newStage,
                request.SequenceNumber,
                newReceivedBytes,
                request.TotalExpectedSize,
                DateTime.UtcNow), 
            cancellationToken);
    }

    private static void EnsureVideoCanReceiveChunks(UploadStatus? currentStatus)
    {
        if (currentStatus?.Stage == UploadStage.Processing)
            throw new InvalidOperationException("Video is currently being processed and cannot receive new chunks");
            
        if (currentStatus?.Stage == UploadStage.Processed)
            throw new InvalidOperationException("Video has already been processed and cannot receive new chunks");
    }
}