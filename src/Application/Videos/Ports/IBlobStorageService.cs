namespace Application.Videos.Ports;

public interface IBlobStorageService
{
    Task UploadChunkAsync(string videoId, int chunkIndex, byte[] chunkData, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadChunkAsync(string videoId, int chunkIndex, CancellationToken cancellationToken = default);
    Task<Stream> GetVideoStreamAsync(string videoId, CancellationToken cancellationToken = default);
    Task DeleteVideoAsync(string videoId, CancellationToken cancellationToken = default);
    Task<bool> VideoExistsAsync(string videoId, CancellationToken cancellationToken = default);
    Task<List<int>> GetUploadedChunksAsync(string videoId, CancellationToken cancellationToken = default);
}