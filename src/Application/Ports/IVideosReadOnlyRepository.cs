using Domain.Common;

namespace Application.Ports;

public interface IVideosReadOnlyRepository
{
    Task<Result> GetIntoLocalFolderAsync(string videoId, string localVideoPath, CancellationToken cancellationToken);
    Task<Result<Stream>> GetAsync(string videoId, string localVideoPath, CancellationToken cancellationToken);
}