using Domain.Videos;

namespace Application.Videos.Ports;

public interface IQrCodeDetector
{
    Task<IReadOnlyCollection<QrCodeDetection>> DetectQrCodesAsync(string videoPath, CancellationToken cancellationToken);
}