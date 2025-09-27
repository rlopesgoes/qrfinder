using Application.Videos.Features.GetVideoResults;
using Application.Videos.Ports.Dtos;
using Domain.Common;

namespace Application.Videos.Ports;

public interface IVideoProcessingRepository
{
    Task<Result<VideoProcessingResult>> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);
    Task SaveAsync(VideoProcessingResult videoProcessing, CancellationToken cancellationToken = default);
}

public record VideoProcessingResult(
    string VideoId,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TotalQrCodes,
    IReadOnlyList<QrCodeResult> QrCodes);