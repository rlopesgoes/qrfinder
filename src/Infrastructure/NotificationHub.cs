using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Hubs;

public sealed class NotificationHub : Hub
{
    public async Task JoinVideoGroup(string videoId)
    {
        var normalizedVideoId = videoId?.Trim().ToLowerInvariant() ?? string.Empty;
        await Groups.AddToGroupAsync(Context.ConnectionId, normalizedVideoId);
    }

    public async Task LeaveVideoGroup(string videoId)
    {
        var normalizedVideoId = videoId?.Trim().ToLowerInvariant() ?? string.Empty;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, normalizedVideoId);
    }
}