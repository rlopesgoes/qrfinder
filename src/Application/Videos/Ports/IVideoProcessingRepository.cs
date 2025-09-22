using Domain.Videos;

namespace Application.Videos.Ports;

public interface IVideoProcessingRepository
{
    Task<VideoProcessing?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);
    Task SaveAsync(VideoProcessing videoProcessing, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string videoId, VideoProcessingStatus status, string? errorMessage = null, CancellationToken cancellationToken = default);
    Task AddQRCodeResultsAsync(string videoId, IEnumerable<QRCodeResult> qrCodes, CancellationToken cancellationToken = default);
}