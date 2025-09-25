namespace Application.Videos.Ports;

/// <summary>
/// Interface for notifying video progress updates
/// </summary>
public interface IVideoProgressNotifier
{
    Task NotifyProgressAsync(string videoId, string stage, double progressPercentage, string? errorMessage = default, CancellationToken cancellationToken = default);
}