// using Application.Videos.Ports;
// using Application.Videos.Ports.Dtos;
// using Domain.Videos;
// using MediatR;
//
// namespace Application.Videos.UseCases.StoreVideoChunk;
//
// public record StoreVideoChunkCommand(string VideoId, byte[] ChunkData, int SequenceNumber, long TotalExpectedSize) : IRequest;
//
// public class StoreVideoChunkHandler(
//     IVideoChunkStorage chunkStorage,
//     IProcessStatusRepository statusRepository) 
//     : IRequestHandler<StoreVideoChunkCommand>
// {
//     public async Task Handle(StoreVideoChunkCommand request, CancellationToken cancellationToken)
//     {
//         try
//         {
//             var videoId = VideoId.From(request.VideoId);
//             var currentStatus = await statusRepository.GetAsync(request.VideoId, cancellationToken);
//             
//             EnsureVideoCanReceiveChunks(currentStatus);
//
//             await chunkStorage.StoreChunkAsync(videoId, request.ChunkData, cancellationToken);
//         }
//         catch (Exception)
//         {
//             await statusRepository.UpsertAsync(
//                 new ProcessStatus(request.VideoId, VideoProcessingStage.Failed), 
//                 cancellationToken);
//             throw;
//         }
//     }
//
//     private static void EnsureVideoCanReceiveChunks(ProcessStatus? currentStatus)
//     {
//         if (currentStatus?.Stage == VideoProcessingStage.Processing)
//             throw new InvalidOperationException("Video is currently being processed and cannot receive new chunks");
//             
//         if (currentStatus?.Stage == VideoProcessingStage.Processed)
//             throw new InvalidOperationException("Video has already been processed and cannot receive new chunks");
//     }
// }