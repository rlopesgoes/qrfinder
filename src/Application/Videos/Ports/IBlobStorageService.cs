namespace Application.Videos.Ports;

public interface IBlobStorageService
{
    Task<string> GenerateUploadUrlAsync(string videoId, CancellationToken cancellationToken = default);
    Task DeleteVideoAsync(string videoId, CancellationToken cancellationToken = default);
    Task<bool> VideoExistsAsync(string videoId, CancellationToken cancellationToken = default);
    Task<string> DownloadVideoDirectlyAsync(string videoId, string outputPath, CancellationToken cancellationToken = default);
}