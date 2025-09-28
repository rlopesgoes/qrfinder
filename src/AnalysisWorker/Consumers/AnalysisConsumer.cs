using System.Text.Json;
using Application.UseCases.ScanQrCode;
using Confluent.Kafka;
using MediatR;

namespace Worker;

public record VideoAnalysisMessage(string VideoId);

public class AnalysisConsumer(
    IConsumer<string, string> consumer,
    IMediator mediator,
    ILogger<AnalysisConsumer> logger) : BackgroundService
{
    private const string TopicControl = "video.analysis.queue";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe(TopicControl);

        while (!stoppingToken.IsCancellationRequested)
        {
            var consumeResult = consumer.Consume(stoppingToken);
            if (consumeResult is null)
                continue;
            
            var message = JsonSerializer.Deserialize<VideoAnalysisMessage>(consumeResult.Message.Value);
            if (message is null)
                continue;
            
            var result = await mediator.Send(new ScanQrCodeCommand(message.VideoId), stoppingToken);
                    
            consumer.Commit(consumeResult);
                    
            if (!result.IsSuccess)
                logger.LogError("Failed to process video {VideoId}: {Error}", message.VideoId, result.Error?.Message);
        }
    }
}