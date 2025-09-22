namespace Application.Videos.Ports;

public interface IVideoUploader
{
    Task UploadAsync(string videoId, long totalBytes, Stream source, IUploadReporter reporter, CancellationToken cancellationToken);
}