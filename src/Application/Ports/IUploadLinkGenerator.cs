using Domain.Common;
using Domain.Models;

namespace Application.Ports;

public interface IUploadLinkGenerator
{
    Task<Result<UploadLink>> GenerateAsync(string videoId, CancellationToken cancellationToken);
}