using System.Text.Json;
using System.Text.Json.Serialization;
using Application.UseCases.SendNotifications;
using Confluent.Kafka;
using Domain.Models;
using MediatR;

namespace NotificationService.Consumers;

public class NotificationConsumer(
    IMediator mediator,
    IConsumer<Ignore, string> consumer,
    ILogger<NotificationConsumer> logger)
    : BackgroundService
{
    private const string VideoProgressNotificationsTopic = "video.progress";
    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    }; 

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting notification consumer");
        
        consumer.Subscribe(VideoProgressNotificationsTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult is null)
                    continue;
                
                var notification = JsonSerializer.Deserialize<Notification>(consumeResult.Message.Value, JsonOptions);
                if (notification is null)
                    continue;
                
                logger.LogInformation("Processing notification for video {VideoId}", notification.VideoId);
                
                var result = await mediator.Send(new SendNotificationsCommand(
                    notification.VideoId,
                    notification.Stage,
                    notification.ProgressPercentage,
                    notification.Message, 
                    notification.Timestamp), stoppingToken);
                    
                consumer.Commit(consumeResult);
                
                logger.LogInformation("Notification processed for video {VideoId}", notification.VideoId);
                
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