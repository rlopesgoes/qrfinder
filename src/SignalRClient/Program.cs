using Microsoft.AspNetCore.SignalR.Client;
using SignalRClient;

var hubUrl = "http://localhost:5010/notificationHub";
var rawId = args.Length > 0 ? args[0] : "adda9fa616f44f7fbf1f657b8f2b8a47";

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
    
    if (!string.IsNullOrEmpty(notification.CurrentOperation))
        message += $" - {notification.CurrentOperation}";
    
    if (!string.IsNullOrEmpty(notification.ErrorMessage))
        message += $" | ERROR: {notification.ErrorMessage}";
    
    message += $" @ {notification.Timestamp:HH:mm:ss}";
    
    Console.WriteLine(message);
});

await conn.StartAsync();
await conn.InvokeAsync("JoinVideoGroup", videoId);  

Console.WriteLine($"🔗 Conectado ao NotificationService em {hubUrl}");
Console.WriteLine("📋 Estágios: UPLOADING → UPLOADED → PROCESSING → PROCESSED | FAILED");
Console.WriteLine("⏹️  Pressione Enter para sair...\n");
Console.ReadLine();

namespace SignalRClient
{
    public record VideoProcessingNotification(
        string VideoId,
        string Stage,
        double? Percent,
        string? CurrentOperation,
        string? ErrorMessage,
        DateTime Timestamp);
}