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
            
            DateTime? startTime = null;
            var startTimeHeader = consumeResult.Message.Headers?.FirstOrDefault(h => h.Key == "x-started-at");
            if (startTimeHeader?.GetValueBytes() != null)
            {
                var startTimeString = System.Text.Encoding.UTF8.GetString(startTimeHeader.GetValueBytes());
                if (DateTime.TryParseExact(startTimeString, "O", null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedStartTime))
                    startTime = parsedStartTime;
            }
            
            var result = await mediator.Send(new ScanQrCodeCommand(message.VideoId, startTime), stoppingToken);
                    
            consumer.Commit(consumeResult);
                    
            if (!result.IsSuccess)
                logger.LogError("Failed to process video {VideoId}: {Error}", message.VideoId, result.Error?.Message);
        }
    }
}