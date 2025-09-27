using Confluent.Kafka;
using NotificationService.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotificationService.Services;

public class KafkaConsumerService(
    IServiceProvider serviceProvider,
    ILogger<KafkaConsumerService> logger)
    : BackgroundService
{
    private readonly string _topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? "video.progress.notifications";
    private readonly ConsumerConfig _consumerConfig = new()
    {
        BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092",
        GroupId = Environment.GetEnvironmentVariable("KAFKA_GROUP_ID") ?? "notification-service", 
        AutoOffsetReset = AutoOffsetReset.Latest,
        EnableAutoCommit = false
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Kafka consumer service started. Topic: {Topic}", _topic);
        
        await Task.Delay(2000, stoppingToken);
        
        using var consumer = new ConsumerBuilder<Ignore, string>(_consumerConfig).Build();
        consumer.Subscribe(_topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    
                    if (consumeResult?.Message?.Value is not null)
                    {
                        await ProcessMessage(consumeResult.Message.Value, stoppingToken);
                        consumer.Commit(consumeResult);
                        
                        logger.LogDebug("Message processed and committed. Offset: {Offset}", consumeResult.Offset);
                    }
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Error consuming message from Kafka");
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
        finally
        {
            consumer.Close();
            logger.LogInformation("Kafka consumer service stopped");
        }
    }

    private async Task ProcessMessage(string messageValue, CancellationToken cancellationToken)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            
            var notification = JsonSerializer.Deserialize<NotificationRequest>(messageValue, options);
            if (notification is null)
            {
                logger.LogWarning("Failed to deserialize notification message: {Message}", messageValue);
                return;
            }

            logger.LogDebug("Processing notification for video {VideoId}: {Stage} - {Progress}%", 
                notification.VideoId, notification.Stage, notification.ProgressPercentage);

            using var scope = serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<NotificationDispatcher>();
            
            await dispatcher.DispatchAsync(notification, cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize notification message: {Message}", messageValue);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process notification message: {Message}", messageValue);
        }
    }
}