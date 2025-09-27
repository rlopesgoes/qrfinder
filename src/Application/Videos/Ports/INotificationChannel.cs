namespace Application.Videos.Ports;

/// <summary>
/// Interface for notification channels (SignalR, Webhooks, SMS, etc.)
/// </summary>
public interface INotificationChannel
{
    string ChannelName { get; }
    Task SendNotificationAsync(NotificationRequest notification, CancellationToken cancellationToken = default);
}