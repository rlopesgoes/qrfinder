using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Hubs;

public sealed class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinVideoGroup(string videoId)
    {
        try
        {
            _logger.LogInformation("JoinVideoGroup called with videoId: {VideoId}", videoId);
            var normalizedVideoId = videoId?.Trim().ToLowerInvariant().Replace("-", "") ?? string.Empty;
            var groupName = $"video_{normalizedVideoId}";
            
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Successfully added to group: {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in JoinVideoGroup for videoId: {VideoId}", videoId);
            throw;
        }
    }

    public async Task LeaveVideoGroup(string videoId)
    {
        var normalizedVideoId = videoId?.Trim().ToLowerInvariant().Replace("-", "") ?? string.Empty;
        var groupName = $"video_{normalizedVideoId}";
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}