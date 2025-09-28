using Domain.Common;

namespace Application.Ports;

public interface IVideoAnalysisQueue
{
    Task<Result> EnqueueAsync(string videoId, CancellationToken cancellationToken);
}
