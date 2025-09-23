namespace Domain.Videos.Ports;

// Unified notification service (replaces IProgressNotifier + IUploadReporter)
public interface INotificationService
{
    Task NotifyUploadStartedAsync(VideoId videoId, long totalBytes, CancellationToken cancellationToken = default);
    Task NotifyUploadProgressAsync(VideoId videoId, int lastSeq, long received, long total, CancellationToken cancellationToken = default);
    Task NotifyUploadCompletedAsync(VideoId videoId, int lastSeq, long received, long total, CancellationToken cancellationToken = default);
    Task NotifyProcessingCompletedAsync(VideoId videoId, ProcessingResult result, CancellationToken cancellationToken = default);
}