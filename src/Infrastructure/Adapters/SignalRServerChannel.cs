using Application.Ports;
using Domain.Common;
using Domain.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Adapters;

public class SignalRServerChannel(ILogger<SignalRServerChannel> logger) 
    : INotificationChannel, IDisposable
{
    private readonly HubConnection _hubConnection = new HubConnectionBuilder()
        .WithUrl(Environment.GetEnvironmentVariable("SIGNALR_HUB_URL") ?? "http://localhost:5010/notificationHub")
        .Build();

    public string ChannelName => "SignalRServer";

    public async Task<Result> SendNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
                await _hubConnection.StartAsync(cancellationToken);
            
            await _hubConnection.SendAsync("SendToGroup", notification.VideoId, "progress", new
            {
                videoId = notification.VideoId,
                stage = notification.Stage.ToString().ToUpperInvariant(),
                percent = notification.ProgressPercentage,
                message = notification.Message,
                timestamp = notification.Timestamp
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send to SignalR Hub for {VideoId}", notification.VideoId);
        }
        
        return Result.Success();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _hubConnection?.DisposeAsync().AsTask().Wait();
    }
}