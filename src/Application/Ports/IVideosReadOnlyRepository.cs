using Domain.Common;
using Domain.Models;

namespace Application.Ports;

public interface IVideosReadOnlyRepository
{
    Task<Result> GetIntoLocalFolderAsync(string videoId, string localVideoPath, CancellationToken cancellationToken);
    Task<Result<Video>> GetAsync(string videoId, CancellationToken cancellationToken);
}