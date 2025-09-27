using Microsoft.AspNetCore.SignalR.Client;
using SignalRClient;

var hubUrl = "http://localhost:5001/notificationHub";
var rawId = args.Length > 0 ? args[0] : "4d4e2eb3-a944-4ec9-ad50-6bebebe9f180";

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