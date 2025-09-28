using Domain.Common;
using Domain.Models;

namespace Application.Ports;

public interface IQrCodeExtractor
{
    Task<Result<QrCodes>> ExtractFromVideoAsync(VideoId videoId, CancellationToken cancellationToken);
}