namespace Domain.Videos.Ports;

/// <summary>
/// Extracts QR codes from uploaded video chunks.
/// Handles all technical details: file assembly, frame extraction, QR detection.
/// </summary>
public interface IQrCodeExtractor
{
    /// <summary>
    /// Extracts all QR codes found in the video.
    /// </summary>
    /// <param name="videoId">Video to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of unique QR codes with timestamps</returns>
    Task<IReadOnlyList<QrCode>> ExtractFromVideoAsync(VideoId videoId, CancellationToken cancellationToken = default);
}