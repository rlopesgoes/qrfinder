using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using Domain.Videos;
using MediatR;

namespace Application.Videos.Features.VideoUploader;

public class VideoUploaderHandler(
    IVideoUploader videoUploader, 
    IVideoStatusRepository videoStatusRepository,
    IUploadReporter uploadReporter) 
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
                new Observer(videoStatusRepository, uploadReporter), cancellationToken);
        }
        catch (Exception)
        {
            await videoStatusRepository.UpsertAsync(
                new UploadStatus(uploaderRequest.VideoId, VideoProcessingStage.Failed, -1, 0, 0, DateTime.UtcNow), 
                cancellationToken);
            throw;
        }
    }

    private sealed class Observer(IVideoStatusRepository videoStatusRepository, IUploadReporter uploadReporter) : IUploadReporter
    {
        public async Task OnStartedAsync(string id, long total, CancellationToken cancellationToken)
        {
            var status = new UploadStatus(id, VideoProcessingStage.Uploading, -1, 0, total, DateTime.UtcNow);
            await videoStatusRepository.UpsertAsync(status, cancellationToken);
            await uploadReporter.OnStartedAsync(id, total, cancellationToken);
        }

        public async Task OnProgressAsync(string id, long lastSeq, long received, long total, CancellationToken cancellationToken)
        {
            var status = new UploadStatus(id, VideoProcessingStage.Uploading, lastSeq, received, total, DateTime.UtcNow);
            await videoStatusRepository.UpsertAsync(status, cancellationToken);
            await uploadReporter.OnProgressAsync(id, lastSeq, received, total, cancellationToken);
        }

        public async Task OnCompletedAsync(string id, long lastSeq, long received, long total, CancellationToken cancellationToken)
        {
            var status = new UploadStatus(id, VideoProcessingStage.Uploaded, lastSeq, received, total, DateTime.UtcNow);
            await videoStatusRepository.UpsertAsync(status, cancellationToken);
            await uploadReporter.OnCompletedAsync(id, lastSeq, received, total, cancellationToken);
        }
    }
}