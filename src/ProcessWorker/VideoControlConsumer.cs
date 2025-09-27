using System.Diagnostics;
using System.Text.Json;
using Application.Videos.UseCases.ProcessVideo;
using Confluent.Kafka;
using Infrastructure.Telemetry;
using MediatR;

namespace Worker;

public record VideoAnalysisMessage(string VideoId);

public class VideoControlConsumer(
    [FromKeyedServices("ControlConsumer")] IConsumer<string, string> consumer,
    IMediator mediator,
    ILogger<VideoControlConsumer> logger) : BackgroundService
{
    private readonly string _topicControl = "video.analysis.queue";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe([_topicControl]);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult is null) continue;

                VideoAnalysisMessage message;
                try
                {
                    message = JsonSerializer.Deserialize<VideoAnalysisMessage>(consumeResult.Message.Value)!;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to parse message: {Message}", consumeResult.Message.Value);
                    continue;
                }

                var videoId = message.VideoId;

                const string messageType = "process";

                // Extract trace context and start new activity
                using var activity = KafkaTraceContextPropagator.ExtractAndStartActivity(
                    consumeResult.Message.Headers, 
                    $"ProcessVideoControl.{messageType}");
                
                activity?.SetTag("video.id", videoId);
                activity?.SetTag("message.type", messageType);

                logger.LogInformation("Received control message for {VideoId}: {MessageType}", videoId, messageType);

                try
                {
                    var result = await mediator.Send(new ProcessVideoCommand(videoId, messageType), stoppingToken);
                    
                    if (result != null)
                    {
                        logger.LogInformation("Processing completed for {VideoId}. QR codes found: {QrCount}", 
                            videoId, result.QrCodes.Count);
                        activity?.SetStatus(ActivityStatusCode.Ok);
                    }
                    else
                    {
                        logger.LogInformation("Processing skipped for {VideoId} (message type: {MessageType})", 
                            videoId, messageType);
                    }

                    consumer.Commit(consumeResult);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing video {VideoId}", videoId);
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    throw;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in VideoControlConsumer: {ex.Message}");
                Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                await Task.Delay(50, stoppingToken);
            }
        }
    }
}