using System.Text.Json;
using Application.Ports;
using Confluent.Kafka;
using Domain.Common;
using Domain.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Adapters;

public class KafkaProgressNotifier(
    IProducer<string, string> producer,
    ILogger<KafkaProgressNotifier> logger) : IProgressNotifier
{
    private const string ProgressTopic = "video.progress";
    
    public async Task<Result> NotifyAsync(ProgressNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var message = new
            {
                VideoId = notification.VideoId,
                Stage = notification.Stage,
                ProgressPercentage = notification.ProgressPercentage,
                Message = notification.Message,
                Timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(message);
            
            await producer.ProduceAsync(ProgressTopic, new Message<string, string>
            {
                Key = notification.VideoId,
                Value = json
            }, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send progress notification for video {VideoId}", notification.VideoId);
            return ex;
        }
    }
}