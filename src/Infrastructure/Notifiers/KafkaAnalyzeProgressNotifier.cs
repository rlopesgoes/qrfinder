using Application.Videos.Ports;
using Confluent.Kafka;
using Domain.Common;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Notifiers;

public class KafkaAnalyzeProgressNotifier(
    IProducer<string, string> producer,
    string progressTopic,
    ILogger<KafkaAnalyzeProgressNotifier> logger) : IAnalyzeProgressNotifier
{
    private readonly string _topic = progressTopic;

    public async Task<Result> NotifyProgressAsync(AnalyzeProgressNotification notification, CancellationToken cancellationToken)
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
            
            await producer.ProduceAsync(_topic, new Message<string, string>
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