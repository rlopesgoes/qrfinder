namespace Domain.Videos.Ports;

// This service encapsulates ALL technical video operations (FFmpeg, file storage, QR detection)
public interface IVideoContentService
{
    // Chunk storage operations (preserving exact original logic)
    Task StoreChunkAsync(VideoId videoId, byte[] chunkData, CancellationToken cancellationToken = default);
    Task<string> FinalizeVideoAsync(VideoId videoId, CancellationToken cancellationToken = default);
    Task CleanupVideoAsync(string videoPath, CancellationToken cancellationToken = default);
    
    // QR Code detection with all original fallbacks preserved
    Task<IReadOnlyList<Domain.Videos.QrCodeDetection>> DetectQrCodesAsync(string videoPath, CancellationToken cancellationToken = default);
}