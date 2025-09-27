using Domain.Common;

namespace Application.Videos.Ports;

public interface IAnalyzeProgressNotifier
{
    Task<Result> NotifyProgressAsync(AnalyzeProgressNotification notification, CancellationToken cancellationToken);
}

public record AnalyzeProgressNotification(
    string VideoId, 
    string Stage, 
    double ProgressPercentage = 0, 
    string? Message = null);