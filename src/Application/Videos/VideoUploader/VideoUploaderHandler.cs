using Application.Videos.Common;
using Application.Videos.Data;
using Application.Videos.Data.Dto;
using Domain;
using MediatR;

namespace Application.Videos.VideoUploader;

public class VideoUploaderHandler(
    IVideoUploader videoUploader, 
    IVideoStatusRepository videoStatusRepository,
    IProgressNotifier progressNotifier) 
    : IRequestHandler<VideoUploaderRequest>
{
    public Task Handle(VideoUploaderRequest uploaderRequest, CancellationToken cancellationToken)
        => videoUploader.UploadAsync(uploaderRequest.VideoId, uploaderRequest.TotalBytes, uploaderRequest.Source,
            new Observer(videoStatusRepository, progressNotifier), cancellationToken);

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