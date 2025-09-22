namespace Application.Videos.Ports;

public interface IVideoProcessingService
{
    Task<VideoProcessingResult> ProcessCompletedVideoAsync(string videoId, CancellationToken cancellationToken);
}

public record VideoProcessingResult(
    string VideoId,
    DateTimeOffset CompletedAt,
    double ProcessingTimeMs,
    IReadOnlyCollection<QrCodeDetection> QrCodes);

public record QrCodeDetection(string Text, string FormattedTimestamp);