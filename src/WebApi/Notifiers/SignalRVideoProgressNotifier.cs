using Application.Videos.Ports;
using Microsoft.AspNetCore.SignalR;
using WebApi.Hubs;

namespace WebApi.Notifiers;

/// <summary>
/// SignalR implementation for video progress notifications
/// </summary>
public class SignalRVideoProgressNotifier(IHubContext<UploadProgressHub> hubContext) : IVideoProgressNotifier
{
    public async Task NotifyProgressAsync(string videoId, string stage, double progressPercentage, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        var videoIdNormalized = videoId?.Trim().ToLowerInvariant() ?? string.Empty;
        
        await hubContext.Clients.Group(videoIdNormalized).SendAsync("progress", new { 
            videoIdNormalized, 
            stage = stage.ToUpperInvariant(), 
            percent = progressPercentage, 
            errorMessage 
        }, cancellationToken);
    }
}