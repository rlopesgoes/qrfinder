using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs;

public sealed class UploadProgressHub : Hub
{
    public Task Join(string videoId) => 
        Groups.AddToGroupAsync(Context.ConnectionId, videoId?.Trim().ToLowerInvariant() ?? string.Empty);
}