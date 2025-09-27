using System.Text.Json;
using Application.Videos.UseCases.ProcessVideo;
using Confluent.Kafka;
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
            var consumeResult = consumer.Consume(stoppingToken);
            
            var message = JsonSerializer.Deserialize<VideoAnalysisMessage>(consumeResult.Message.Value)!;
            var result = await mediator.Send(new ScanQrCodeCommand(message.VideoId), stoppingToken);
                    
            consumer.Commit(consumeResult);
                    
            if (!result.IsSuccess)
                logger.LogError("Failed to process video {VideoId}: {Error}", message.VideoId, result.Error?.Message);
        }
    }
}