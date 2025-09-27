using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NotificationService.Hubs;
using NotificationService.Services;
using NotificationService.Models;

namespace NotificationService.Channels;

public class SignalRServerChannel(IHubContext<NotificationHub> hubContext, ILogger<SignalRServerChannel> logger)
    : INotificationChannel
{
    public string ChannelName => "SignalRServer";

    public async Task SendNotificationAsync(NotificationRequest notification, CancellationToken cancellationToken = default)
    {
        var videoIdNormalized = notification.VideoId?.Trim().ToLowerInvariant().Replace("-", "") ?? string.Empty;
        
        await hubContext.Clients.Group($"video_{videoIdNormalized}").SendAsync("progress", new
        {
            videoId = notification.VideoId,
            stage = notification.Stage.ToString().ToUpperInvariant(),
            percent = notification.ProgressPercentage,
            message = notification.Message,
            timestamp = notification.Timestamp
        }, cancellationToken);

        logger.LogDebug("Notification sent to SignalR group {VideoId}: {Stage} - {Progress}%", 
            videoIdNormalized, notification.Stage, notification.ProgressPercentage);
    }
}