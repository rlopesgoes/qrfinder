using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using Domain.Videos;
using MediatR;

namespace Application.Videos.Features.VideoUploader;

public class VideoUploaderHandler(
    IVideoUploader videoUploader, 
    IVideoStatusRepository videoStatusRepository,
    IProgressNotifier progressNotifier) 
    : IRequestHandler<VideoUploaderRequest>
{
    public async Task Handle(VideoUploaderRequest uploaderRequest, CancellationToken cancellationToken)
    {
        var existingStatus = await videoStatusRepository.GetAsync(uploaderRequest.VideoId, cancellationToken);
        
        if (existingStatus?.Stage == UploadStage.Uploaded)
            throw new InvalidOperationException("Video has already been uploaded");
            
        if (existingStatus?.Stage == UploadStage.Processing || existingStatus?.Stage == UploadStage.Processed)
            throw new InvalidOperationException("Cannot re-upload a video that is being or has been processed");

        await videoUploader.UploadAsync(uploaderRequest.VideoId, uploaderRequest.TotalBytes, uploaderRequest.Source,
            new Observer(videoStatusRepository, progressNotifier), cancellationToken);
    }

    private sealed class Observer(IVideoStatusRepository videoStatusRepository, IProgressNotifier progressNotifier) : IUploadReporter
    {
        public async Task OnStartedAsync(string id, long total, CancellationToken cancellationToken)
        {
            var status = new UploadStatus(id, UploadStage.Uploading, -1, 0, total, DateTime.UtcNow);
            await videoStatusRepository.UpsertAsync(status, cancellationToken);
            await progressNotifier.NotifyStartedAsync(id, total, cancellationToken);
        }

        public async Task OnProgressAsync(string id, long lastSeq, long received, long total, CancellationToken cancellationToken)
        {
            var status = new UploadStatus(id, UploadStage.Uploading, lastSeq, received, total, DateTime.UtcNow);
            await videoStatusRepository.UpsertAsync(status, cancellationToken);
            await progressNotifier.NotifyProgressAsync(id, lastSeq, received, total, cancellationToken);
        }

        public async Task OnCompletedAsync(string id, long lastSeq, long received, long total, CancellationToken cancellationToken)
        {
            var status = new UploadStatus(id, UploadStage.Uploaded, lastSeq, received, total, DateTime.UtcNow);
            await videoStatusRepository.UpsertAsync(status, cancellationToken);
            await progressNotifier.NotifyCompletedAsync(id, lastSeq, received, total, cancellationToken);
        }
    }
}