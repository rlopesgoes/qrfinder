using Confluent.Kafka;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.UseCases.SendNotifications;
using Application.Videos.Ports;
using MediatR;
using VideoProcessingStage = Domain.Videos.VideoProcessingStage;

namespace NotificationService.Services;

public class KafkaConsumerService(
    IMediator mediator,
    IConsumer<Ignore, string> consumer,
    string topic,
    ILogger<KafkaConsumerService> logger)
    : BackgroundService
{
    private readonly string _topic = topic;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Kafka consumer service started. Topic: {Topic}", _topic);
        
        await Task.Delay(2000, stoppingToken);
        
        consumer.Subscribe(_topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult is null)
                    continue;
                    
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                var notification = JsonSerializer.Deserialize<NotificationRequest>(consumeResult.Message.Value, options);
                if (notification is null)
                    continue;
                var result = await mediator.Send(new SendNotificationsCommand(
                    notification.VideoId,
                    (VideoProcessingStage)notification.Stage,
                    notification.ProgressPercentage,
                    notification.Message, notification.Timestamp), stoppingToken);
                    
                consumer.Commit(consumeResult);
                
                if (!result.IsSuccess)
                    logger.LogError("Failed to process notification for video {VideoId}: {Error}", notification.VideoId, result.Error?.Message);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing Kafka message");
            }
        }
    }
}