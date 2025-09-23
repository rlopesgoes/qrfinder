using Domain.Videos;
using Domain.Videos.Ports;
using Confluent.Kafka;
using System.Text.Json;

namespace Infrastructure.Videos;

// Unified notification service that adapts to existing infrastructure
public class NotificationService : INotificationService
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly string _topicResults = "videos.results";

    public NotificationService(IProducer<string, byte[]> producer)
    {
        _producer = producer;
    }

    public Task NotifyUploadStartedAsync(VideoId videoId, long totalBytes, CancellationToken cancellationToken = default)
    {
        // Legacy progress notification can be handled elsewhere if needed
        return Task.CompletedTask;
    }

    public Task NotifyUploadProgressAsync(VideoId videoId, int lastSeq, long received, long total, CancellationToken cancellationToken = default)
    {
        // Legacy progress notification can be handled elsewhere if needed
        return Task.CompletedTask;
    }

    public Task NotifyUploadCompletedAsync(VideoId videoId, int lastSeq, long received, long total, CancellationToken cancellationToken = default)
    {
        // Legacy progress notification can be handled elsewhere if needed
        return Task.CompletedTask;
    }

    public async Task NotifyProcessingCompletedAsync(VideoId videoId, ProcessingResult result, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[DEBUG] NotificationService.NotifyProcessingCompletedAsync called for videoId={videoId}");
        Console.WriteLine($"[DEBUG] QR codes count: {result.QrCodes.Count}");
        
        // Send to Kafka results topic (preserving exact original JSON structure)
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            videoId = videoId.ToString(),
            completedAt = DateTimeOffset.UtcNow,
            processingTimeMs = result.Metrics.ProcessingTimeMs,
            codes = result.QrCodes.Select(qr => new
            {
                text = qr.Content,
                timestamp = qr.FormattedTimestamp
            }).ToArray()
        });

        Console.WriteLine($"[DEBUG] Sending to Kafka topic: {_topicResults}");
        
        await _producer.ProduceAsync(_topicResults,
            new Message<string, byte[]> { Key = videoId.ToString(), Value = payload }, cancellationToken);
            
        Console.WriteLine($"[DEBUG] Message sent to Kafka successfully");
    }
}