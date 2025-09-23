using Application.Videos.Ports;
using Microsoft.AspNetCore.SignalR;
using WebApi.Hubs;

namespace WebApi.Notifiers;

public sealed class SignalRProgressNotifier(IHubContext<UploadProgressHub> hub) : IUploadReporter
{
    public Task OnStartedAsync(string videoId, long totalBytes, CancellationToken cancellationToken)
    {
        var videoIdNormalized = videoId?.Trim().ToLowerInvariant() ?? string.Empty;
        return hub.Clients.Group(videoIdNormalized).SendAsync("started",
            new
            {
                videoIdNormalized,
                stage = "UPLOADING",
                totalBytes, 
                percent = 0.0
            }, cancellationToken);
    }

    public Task OnProgressAsync(string videoId, long lastSeq, long receivedBytes, long totalBytes, CancellationToken cancellationToken)
    {
        var videoIdNormalized = videoId?.Trim().ToLowerInvariant() ?? string.Empty;
        return hub.Clients.Group(videoIdNormalized).SendAsync("progress",
            new
            {
                videoIdNormalized,
                stage = "UPLOADING",
                lastSeq, receivedBytes, totalBytes,
                percent = totalBytes > 0 ? (double)receivedBytes / totalBytes * 100.0 : (double?)null
            }, cancellationToken);
    }

    public Task OnCompletedAsync(string videoId, long lastSeq, long receivedBytes, long totalBytes, CancellationToken cancellationToken)
    {
        var videoIdNormalized = videoId?.Trim().ToLowerInvariant() ?? string.Empty;
        return hub.Clients.Group(videoIdNormalized).SendAsync("completed",
            new
            {
                videoIdNormalized,
                stage = "UPLOADED",
                lastSeq, receivedBytes, totalBytes,
                percent = 100.0
            }, cancellationToken);
    }
}