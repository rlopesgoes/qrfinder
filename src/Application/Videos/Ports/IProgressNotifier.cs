namespace Application.Videos.Ports;

public interface IProgressNotifier
{
    Task NotifyStartedAsync(string videoId, long totalBytes, CancellationToken cancellationToken);
    Task NotifyProgressAsync(string videoId, long lastSeq, long receivedBytes, long totalBytes, CancellationToken cancellationToken);
    Task NotifyCompletedAsync(string videoId, long lastSeq, long receivedBytes, long totalBytes, CancellationToken cancellationToken);
}