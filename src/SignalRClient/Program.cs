using Microsoft.AspNetCore.SignalR.Client;
using SignalRClient;

var hubUrl = "http://localhost:5010/hubs/upload";
var rawId = args.Length > 0 ? args[0] : "35578a7c8d074ef19d05e66476daf66f";

var videoId = rawId.Trim().ToLowerInvariant();

var conn = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

conn.On<Started>("started", m =>
    Console.WriteLine($"[{m.VideoId}] START {m.TotalBytes} bytes, stage={m.Stage}"));

conn.On<Progress>("progress", m =>
    Console.WriteLine($"[{m.VideoId}] PROG seq={m.LastSeq} {m.ReceivedBytes}/{m.TotalBytes} ({m.Percent:0.00}%)"));

conn.On<Completed>("completed", m =>
    Console.WriteLine($"[{m.VideoId}] DONE seq={m.LastSeq} total={m.TotalBytes} ({m.Percent:0.00}%)"));

await conn.StartAsync();
await conn.InvokeAsync("Join", videoId);  

Console.WriteLine($"Conectado ao SignalR. Aguardando eventos de {videoId}. Enter para sair.");
Console.ReadLine();

namespace SignalRClient
{
    public record Started(string VideoId, string Stage, long TotalBytes, double? Percent);
    public record Progress(string VideoId, string Stage, long LastSeq, long ReceivedBytes, long TotalBytes, double? Percent);
    public record Completed(string VideoId, string Stage, long LastSeq, long ReceivedBytes, long TotalBytes, double Percent);
}