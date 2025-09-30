using Domain.Common;

namespace Application.Ports;

public interface IVideosWriteOnlyRepository
{
    Task<Result> DeleteAsync(string videoId, CancellationToken cancellationToken);
}