using System.Text.Json;
using Application.Ports;
using Confluent.Kafka;
using Domain.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Adapters;

internal sealed class KafkaVideoAnalysisQueue(
    IProducer<string, string> producer,
    ILogger<KafkaVideoAnalysisQueue> logger)
    : IVideoAnalysisQueue
{
    private const string Topic = "video.analysis.queue";

    public async Task<Result> EnqueueAsync(string videoId, CancellationToken cancellationToken)
    {
        try
        {
            var message = new
            {
                VideoId = videoId
            };

            var json = JsonSerializer.Serialize(message);

            await producer.ProduceAsync(Topic, new Message<string, string>
            {
                Key = videoId,
                Value = json
            }, cancellationToken);
            
            return Result.Success();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to enqueue video {VideoId} for analysis", videoId);
            return e;
        }
    }
}