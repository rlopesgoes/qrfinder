using Microsoft.AspNetCore.SignalR;

namespace SignalRServer.Hubs;

public sealed class NotificationHub(ILogger<NotificationHub> logger) : Hub
{
    public async Task JoinVideoGroup(string videoId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, videoId);
        logger.LogInformation("Successfully added to group: {GroupName}", videoId);
    }

    public async Task LeaveVideoGroup(string videoId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, videoId);
        logger.LogInformation("Successfully removed from group: {GroupName}", videoId);
    }

    public async Task SendToGroup(string videoId, string method, object data)
    {
        await Clients.Group(videoId).SendAsync(method, data);
        logger.LogInformation("Sent message to group: {GroupName} - Method: {Method}", videoId, method);
    }
}