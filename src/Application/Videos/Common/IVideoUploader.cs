namespace Application.Videos.Common;

public interface IVideoUploader
{
    Task UploadAsync(string videoId, long totalBytes, Stream source, IUploadReporter reporter, CancellationToken cancellationToken);
}