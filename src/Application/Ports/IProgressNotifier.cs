using Domain.Common;
using Domain.Models;

namespace Application.Ports;

public interface IProgressNotifier
{
    Task<Result> NotifyAsync(ProgressNotification notification, CancellationToken cancellationToken);
}