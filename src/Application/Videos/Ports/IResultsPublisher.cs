namespace Application.Videos.Ports;

/// <summary>
/// Publishes video processing results to external systems
/// </summary>
public interface IResultsPublisher
{
    /// <summary>
    /// Publishes processing results for a video
    /// </summary>
    Task PublishResultsAsync(string videoId, object results, CancellationToken cancellationToken = default);
}