using Domain.Common;

namespace Application.Videos.Ports;

public interface IUploadLinkGenerator
{
    Task<Result<UploadLink>> GenerateAsync(string videoId, CancellationToken cancellationToken);
}

public record UploadLink(string Url, DateTimeOffset ExpiresAt);