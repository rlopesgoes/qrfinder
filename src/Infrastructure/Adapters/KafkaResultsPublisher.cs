using System.Text.Json;
using Application.Ports;
using Confluent.Kafka;
using Domain.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Adapters;

public class KafkaResultsPublisher(
    IProducer<string, byte[]> producer,
    ILogger<KafkaResultsPublisher> logger) : IResultsPublisher
{
    private const string ResultsTopic = "videos.results";

    public async Task<Result> PublishResultsAsync(string videoId, object results, CancellationToken cancellationToken = default)
    {
        try
        {
            var messageBytes = JsonSerializer.SerializeToUtf8Bytes(results);
            
            await producer.ProduceAsync(ResultsTopic, 
                new Message<string, byte[]>
                {
                    Key = videoId,
                    Value = messageBytes
                }, 
                cancellationToken);
        
            return Result.Success();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to publish results for video {VideoId}", videoId);
            return e;
        }
    }
}