using System.Text.Json;
using Application.Videos.Ports;
using Confluent.Kafka;

namespace Infrastructure.Videos;

public class KafkaResultsPublisher : IResultsPublisher
{
    private readonly IProducer<string, byte[]> _producer;
    private const string ResultsTopic = "videos.results";

    public KafkaResultsPublisher(IProducer<string, byte[]> producer)
    {
        _producer = producer;
    }

    public async Task PublishResultsAsync(string videoId, object results, CancellationToken cancellationToken = default)
    {
        var messageBytes = JsonSerializer.SerializeToUtf8Bytes(results);
        await _producer.ProduceAsync(ResultsTopic, 
            new Message<string, byte[]> { Key = videoId, Value = messageBytes }, 
            cancellationToken);
    }
}