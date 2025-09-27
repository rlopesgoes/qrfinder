using Domain.Common;

namespace Application.Videos.Ports;

public interface IVideoAnalysisQueue
{
    Task<Result> EnqueueAsync(string videoId, CancellationToken cancellationToken);
}
