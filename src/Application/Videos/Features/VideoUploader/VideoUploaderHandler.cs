using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using Application.Videos.Services;
using Domain.Videos;
using MediatR;

namespace Application.Videos.Features.VideoUploader;

public class VideoUploaderHandler(
    IVideoUploader videoUploader, 
    IVideoStatusRepository videoStatusRepository,
    IUploadReporter uploadReporter,
    VideoProgressService progressService) 
    : IRequestHandler<VideoUploaderRequest>
{
    public async Task Handle(VideoUploaderRequest uploaderRequest, CancellationToken cancellationToken)
    {
        try
        {
            var existingStatus = await videoStatusRepository.GetAsync(uploaderRequest.VideoId, cancellationToken);
            
            if (existingStatus?.Stage == VideoProcessingStage.Uploaded)
                throw new InvalidOperationException("Video has already been uploaded");
                
            if (existingStatus?.Stage == VideoProcessingStage.Processing || existingStatus?.Stage == VideoProcessingStage.Processed)
                throw new InvalidOperationException("Cannot re-upload a video that is being or has been processed");

            await videoUploader.UploadAsync(uploaderRequest.VideoId, uploaderRequest.TotalBytes, uploaderRequest.Source,
                new Observer(videoStatusRepository, uploadReporter, progressService), cancellationToken);
        }
        catch (Exception)
        {
            await videoStatusRepository.UpsertAsync(
                new UploadStatus(uploaderRequest.VideoId, VideoProcessingStage.Failed, -1, 0, 0, DateTime.UtcNow), 
                cancellationToken);
            throw;
        }
    }

    private sealed class Observer(IVideoStatusRepository videoStatusRepository, IUploadReporter uploadReporter, VideoProgressService progressService) : IUploadReporter
    {
        public async Task OnStartedAsync(string id, long total, CancellationToken cancellationToken)
        {
            var status = new UploadStatus(id, VideoProcessingStage.Uploading, -1, 0, total, DateTime.UtcNow);
            await videoStatusRepository.UpsertAsync(status, cancellationToken);
            
            // Send notification to Kafka
            await progressService.StartUploadingAsync(id, cancellationToken);
            
           // await uploadReporter.OnStartedAsync(id, total, cancellationToken);
        }

        public async Task OnProgressAsync(string id, long lastSeq, long received, long total, CancellationToken cancellationToken)
        {
            var status = new UploadStatus(id, VideoProcessingStage.Uploading, lastSeq, received, total, DateTime.UtcNow);
            await videoStatusRepository.UpsertAsync(status, cancellationToken);
            
            // Send progress notification to Kafka
            var progressPercent = total > 0 ? (double)received / total * 50.0 : 0.0; // Upload is 0-50%
            await progressService.UpdateStatusAsync(id, VideoProcessingStage.Uploading, progressPercent, $"Uploading {received}/{total} bytes", null, cancellationToken);
            
           // await uploadReporter.OnProgressAsync(id, lastSeq, received, total, cancellationToken);
        }

        public async Task OnCompletedAsync(string id, long lastSeq, long received, long total, CancellationToken cancellationToken)
        {
            var status = new UploadStatus(id, VideoProcessingStage.Uploaded, lastSeq, received, total, DateTime.UtcNow);
            await videoStatusRepository.UpsertAsync(status, cancellationToken);
            
            // Send completion notification to Kafka
            await progressService.CompleteUploadAsync(id, cancellationToken);
            
            //await uploadReporter.OnCompletedAsync(id, lastSeq, received, total, cancellationToken);
        }
    }
}