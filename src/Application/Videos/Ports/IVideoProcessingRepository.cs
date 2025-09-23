using Application.Videos.Ports.Dtos;

namespace Application.Videos.Ports;

public interface IVideoProcessingRepository
{
    Task<VideoProcessingResult?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);
    Task SaveAsync(VideoProcessingResult videoProcessing, CancellationToken cancellationToken = default);
}

public record VideoProcessingResult(
    string VideoId,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TotalQrCodes,
    IReadOnlyList<QrCodeResultDto> QrCodes);