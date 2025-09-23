using Application.Videos.UseCases.StoreVideoChunk;
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
                var sequenceNumber = (int)(consumeResult.Message.Headers.GetInt64("sequence") ?? 0);
                var totalSize = consumeResult.Message.Headers.GetInt64("total-size") ?? chunkData.Length;

                await mediator.Send(new StoreVideoChunkCommand(videoId, chunkData, sequenceNumber, totalSize), stoppingToken);
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