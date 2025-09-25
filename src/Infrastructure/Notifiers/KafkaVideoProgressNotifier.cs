using Application.Videos.Ports;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Notifiers;

/// <summary>
/// Kafka adapter for video progress notifications
/// Sends notifications to Kafka topic for consumption by NotificationService
/// </summary>
public class KafkaVideoProgressNotifier : IVideoProgressNotifier, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _topic;
    private readonly ILogger<KafkaVideoProgressNotifier> _logger;

    public KafkaVideoProgressNotifier(IConfiguration configuration, ILogger<KafkaVideoProgressNotifier> logger)
    {
        _logger = logger;
        _topic = configuration.GetValue<string>("Kafka:Topic") ?? "video-notifications";
        
        var bootstrap = configuration.GetConnectionString("Kafka") ?? "localhost:9092";
        
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrap,
            Acks = Acks.All,
            MessageTimeoutMs = 30000,
        };

        _producer = new ProducerBuilder<Null, string>(config).Build();
        
        _logger.LogInformation("Kafka video progress notifier initialized. Topic: {Topic}", _topic);
    }

    public async Task NotifyProgressAsync(string videoId, string stage, double progressPercentage, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        // Parse stage string to enum
        if (!Enum.TryParse<VideoProcessingStage>(stage, true, out var stageEnum))
        {
            stageEnum = VideoProcessingStage.Processing; // Default fallback
        }

        var notification = new
        {
            VideoId = videoId?.Trim().ToLowerInvariant() ?? string.Empty,
            Stage = (int)stageEnum, // Send as int for JSON serialization
            ProgressPercentage = progressPercentage,
            CurrentOperation = (string?)null,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            var message = JsonSerializer.Serialize(notification);
            
            var result = await _producer.ProduceAsync(_topic, new Message<Null, string> 
            { 
                Value = message 
            }, cancellationToken);

            _logger.LogDebug("Video progress notification sent to Kafka. VideoId: {VideoId}, Stage: {Stage}, Topic: {Topic}, Partition: {Partition}, Offset: {Offset}", 
                notification.VideoId, stageEnum, result.Topic, result.Partition, result.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send video progress notification to Kafka topic {Topic} for video {VideoId}", _topic, videoId);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}

public enum VideoProcessingStage
{
    Uploading = 0,
    Uploaded = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4
}