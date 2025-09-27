using Microsoft.AspNetCore.SignalR.Client;
using SignalRClient;

var hubUrl = "http://localhost:5010/notificationHub";
var rawId = args.Length > 0 ? args[0] : "e8f174d8-3026-4b50-ab31-38b8de5c5776";

var videoId = rawId.Trim().ToLowerInvariant();

var conn = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

conn.On<VideoProcessingNotification>("progress", notification =>
{
    var statusIcon = notification.Stage switch
    {
        "UPLOADING" => "📤",
        "UPLOADED" => "✅",
        "PROCESSING" => "⚙️",
        "PROCESSED" => "🎉",
        "FAILED" => "❌",
        _ => "📡"
    };

    var message = $"{statusIcon} [{notification.VideoId}] {notification.Stage}";
    
    if (notification.Percent.HasValue)
        message += $" ({notification.Percent:0.0}%)";
    
    if (!string.IsNullOrEmpty(notification.Message))
        message += $" | MESSAGE: {notification.Message}";
    
    message += $" @ {notification.Timestamp:HH:mm:ss}";
    
    Console.WriteLine(message);
});

await conn.StartAsync();
await conn.InvokeAsync("JoinVideoGroup", videoId);  

Console.WriteLine($"🔗 Conectado ao NotificationsWorker em {hubUrl}");
Console.WriteLine("📋 Estágios: UPLOADING → UPLOADED → PROCESSING → PROCESSED | FAILED");
Console.WriteLine("⏹️  Pressione Enter para sair...\n");
Console.ReadLine();

namespace SignalRClient
{
    public record VideoProcessingNotification(
        string VideoId,
        string Stage,
        double? Percent,
        string? Message,
        DateTime Timestamp);
}