using Domain.Videos;

namespace Application.Videos.Ports;

/// <summary>
/// Stores video chunks for later assembly
/// </summary>
public interface IVideoChunkStorage
{
    /// <summary>
    /// Stores a chunk of video data
    /// </summary>
    Task StoreChunkAsync(VideoId videoId, byte[] chunkData, CancellationToken cancellationToken = default);
}