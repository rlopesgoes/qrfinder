using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using NotificationService.Services;
using NotificationService.Models;

namespace NotificationService.Channels;

public class SignalRServerChannel : INotificationChannel, IDisposable
{
    private readonly ILogger<SignalRServerChannel> _logger;
    private readonly HubConnection _hubConnection;

    public string ChannelName => "SignalRServer";

    public SignalRServerChannel(ILogger<SignalRServerChannel> logger)
    {
        _logger = logger;
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5010/notificationHub")
            .Build();
    }

    public async Task SendNotificationAsync(NotificationRequest notification, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync(cancellationToken);
            }

            // Send to SignalR Server's Hub directly to group
            await _hubConnection.SendAsync("SendToGroup", notification.VideoId, "progress", new
            {
                videoId = notification.VideoId,
                stage = notification.Stage.ToString().ToUpperInvariant(),
                percent = notification.ProgressPercentage,
                message = notification.Message,
                timestamp = notification.Timestamp
            }, cancellationToken);

            _logger.LogInformation("Sent to SignalR Hub: {VideoId} - {Stage}", notification.VideoId, notification.Stage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send to SignalR Hub for {VideoId}", notification.VideoId);
        }
    }

    public void Dispose()
    {
        _hubConnection?.DisposeAsync().AsTask().Wait();
    }
}