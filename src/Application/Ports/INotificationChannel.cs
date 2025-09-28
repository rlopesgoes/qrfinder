using Domain.Common;
using Domain.Models;

namespace Application.Ports;

public interface INotificationChannel
{
    string ChannelName { get; }
    Task<Result> SendNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
}