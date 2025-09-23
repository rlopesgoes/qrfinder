using Application.Videos.UseCases.ProcessVideo;
using Confluent.Kafka;
using MediatR;

namespace Worker;

public class VideoControlConsumer(
    [FromKeyedServices("ControlConsumer")] IConsumer<string, byte[]> consumer,
    IMediator mediator) : BackgroundService
{
    private readonly string _topicControl = "videos.control";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe([_topicControl]);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult is null) continue;

                var videoId = consumeResult.Message.Key!;
                var messageType = consumeResult.Message.Headers.GetUtf8("type");

                Console.WriteLine($"[DEBUG] Received control message: videoId={videoId}, messageType={messageType}");

                // Process video and let NotificationService handle Kafka publishing
                var result = await mediator.Send(new ProcessVideoCommand(videoId, messageType), stoppingToken);
                
                Console.WriteLine($"[DEBUG] Processing result: {(result != null ? "SUCCESS" : "NULL")}");
                if (result != null)
                {
                    Console.WriteLine($"[DEBUG] QR codes found: {result.QrCodes.Count}");
                }

                consumer.Commit(consumeResult);
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