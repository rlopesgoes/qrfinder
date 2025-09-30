using Domain.Common;

namespace Application.Ports;

public interface IResultsPublisher
{
    Task<Result> PublishResultsAsync(string videoId, object results, CancellationToken cancellationToken);
}