using Domain.Common;
using Domain.Models;

namespace Application.Ports;

public interface IQrCodeScanner
{
    Task<Result<QrCodes>> ScanAsync(Video video, CancellationToken cancellationToken);
}