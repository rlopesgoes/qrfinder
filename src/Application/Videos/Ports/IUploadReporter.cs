namespace Application.Videos.Ports;

public interface IUploadReporter
{
    Task OnStartedAsync(string videoId, long totalBytes, CancellationToken cancellationToken);
    Task OnProgressAsync(string videoId, long lastSeq, long receivedBytes, long totalBytes, CancellationToken cancellationToken);
    Task OnCompletedAsync(string videoId, long lastSeq, long receivedBytes, long totalBytes, CancellationToken cancellationToken);
}