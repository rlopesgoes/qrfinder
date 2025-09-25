using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;
using NotificationService.Models;
using NotificationService.Services;

namespace NotificationService.Channels;

public class SignalRServerChannel(IHubContext<NotificationHub> hubContext, ILogger<SignalRServerChannel> logger)
    : INotificationChannel
{
    public string ChannelName => "SignalRServer";

    public async Task SendNotificationAsync(NotificationRequest notification, CancellationToken cancellationToken = default)
    {
        var videoIdNormalized = notification.VideoId?.Trim().ToLowerInvariant() ?? string.Empty;
        
        await hubContext.Clients.Group(videoIdNormalized).SendAsync("progress", new
        {
            videoId = videoIdNormalized,
            stage = notification.Stage.ToString().ToUpperInvariant(),
            percent = notification.ProgressPercentage,
            currentOperation = notification.CurrentOperation,
            errorMessage = notification.ErrorMessage,
            timestamp = notification.Timestamp
        }, cancellationToken);

        logger.LogDebug("Notification sent to SignalR group {VideoId}: {Stage} - {Progress}%", 
            videoIdNormalized, notification.Stage, notification.ProgressPercentage);
    }
}