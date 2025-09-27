// using Application.Videos.Ports;
// using Application.Videos.Ports.Dtos;
// using Application.Videos.Services;
// using Domain.Videos;
// using MediatR;
//
// namespace Application.Videos.Features.VideoUploader;
//
// public class VideoUploaderHandler(
//     IVideoUploader videoUploader, 
//     IProcessStatusRepository processStatusRepository,
//     VideoProgressService progressService,
//     IBlobStorageService service) 
//     : IRequestHandler<VideoUploaderRequest>
// {
//     public async Task Handle(VideoUploaderRequest uploaderRequest, CancellationToken cancellationToken)
//     {
//         try
//         {
//             var teste = await service.GetUploadedChunksAsync(Guid.NewGuid().ToString(), cancellationToken); // Ensure storage is ready (e.g. create container if not exists)
//             
//             var existingStatus = await processStatusRepository.GetAsync(uploaderRequest.VideoId, cancellationToken);
//             
//             if (existingStatus?.Stage == VideoProcessingStage.Uploaded)
//                 throw new InvalidOperationException("Video has already been uploaded");
//                 
//             if (existingStatus?.Stage == VideoProcessingStage.Processing || existingStatus?.Stage == VideoProcessingStage.Processed)
//                 throw new InvalidOperationException("Cannot re-upload a video that is being or has been processed");
//
//             await videoUploader.UploadAsync(uploaderRequest.VideoId, uploaderRequest.TotalBytes, uploaderRequest.Source,
//                 new Observer(processStatusRepository, progressService), cancellationToken);
//         }
//         catch (Exception)
//         {
//             await processStatusRepository.UpsertAsync(
//                 new ProcessStatus(uploaderRequest.VideoId, VideoProcessingStage.Failed, -1, 0, 0, DateTime.UtcNow), 
//                 cancellationToken);
//             throw;
//         }
//     }
//
//     private sealed class Observer(IProcessStatusRepository processStatusRepository, VideoProgressService progressService) : IUploadReporter
//     {
//         public async Task OnStartedAsync(string id, long total, CancellationToken cancellationToken)
//         {
//             var status = new ProcessStatus(id, VideoProcessingStage.Uploading, -1, 0, total, DateTime.UtcNow);
//             await processStatusRepository.UpsertAsync(status, cancellationToken);
//             
//             // Send notification to Kafka
//             await progressService.StartUploadingAsync(id, cancellationToken);
//         }
//
//         public async Task OnProgressAsync(string id, long lastSeq, long received, long total, CancellationToken cancellationToken)
//         {
//             var status = new ProcessStatus(id, VideoProcessingStage.Uploading, lastSeq, received, total, DateTime.UtcNow);
//             await processStatusRepository.UpsertAsync(status, cancellationToken);
//             
//             // Send progress notification to Kafka
//             var progressPercent = total > 0 ? (double)received / total * 50.0 : 0.0; // Upload is 0-50%
//             await progressService.UpdateStatusAsync(id, VideoProcessingStage.Uploading, progressPercent, $"Uploading {received}/{total} bytes", null, cancellationToken);
//         }
//
//         public async Task OnCompletedAsync(string id, long lastSeq, long received, long total, CancellationToken cancellationToken)
//         {
//             var status = new ProcessStatus(id, VideoProcessingStage.Uploaded, lastSeq, received, total, DateTime.UtcNow);
//             await processStatusRepository.UpsertAsync(status, cancellationToken);
//             
//             // Send completion notification to Kafka
//             await progressService.CompleteUploadAsync(id, cancellationToken);
//         }
//     }
// }