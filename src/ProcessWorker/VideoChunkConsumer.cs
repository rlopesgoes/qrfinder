using Application.Videos.Features.RebuildVideo;
using Confluent.Kafka;
using MediatR;

namespace Worker;

public class VideoChunkConsumer(
    [FromKeyedServices("ChunkConsumer")] IConsumer<string, byte[]> consumer,
    IMediator mediator) : BackgroundService
{
    private readonly string _topicChunks = "videos.raw-chunks";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe([_topicChunks]);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult is null) continue;

                var videoId = consumeResult.Message.Key!;
                var chunkData = consumeResult.Message.Value;

                await mediator.Send(new RebuildVideoRequest(videoId, chunkData), stoppingToken);
                consumer.Commit(consumeResult);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                await Task.Delay(50, stoppingToken);
            }
        }
    }
}