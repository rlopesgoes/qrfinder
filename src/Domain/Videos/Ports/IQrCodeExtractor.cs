using Domain.Common;

namespace Domain.Videos.Ports;

public interface IQrCodeExtractor
{
    Task<Result<QrCodes>> ExtractFromVideoAsync(VideoId videoId, CancellationToken cancellationToken);
}

public record QrCodes(IReadOnlyList<QrCode> Values);