using System.Text.Json;
using Application.Videos.Features.ScanQrCode;
using Confluent.Kafka;
using MediatR;

namespace Worker;

public class VideoControlConsumer(
    [FromKeyedServices("ControlConsumer")] IConsumer<string, byte[]> consumer,
    IProducer<string, byte[]> producer,
    IMediator mediator) : BackgroundService
{
    private readonly string _topicControl = "videos.control";
    private readonly string _topicResults = "videos.results";

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

                var result = await mediator.Send(new ScanQrCodeRequest(videoId, messageType), stoppingToken);
                if (result != null)
                {
                    var payload = SerializeResult(result);
                    await SendResultsAsync(videoId, payload, stoppingToken);
                }

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

    private static byte[] SerializeResult(ProcessVideoResult result) =>
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            videoId = result.VideoId,
            completedAt = result.CompletedAt,
            processingTimeMs = result.ProcessingTimeMs,
            codes = result.QrCodes.Select(qr => new
            {
                text = qr.Text,
                timestamp = qr.FormattedTimestamp
            }).ToArray()
        });

    private async Task SendResultsAsync(string videoId, byte[] payload, CancellationToken cancellationToken)
        => await producer.ProduceAsync(_topicResults,
            new Message<string, byte[]> { Key = videoId, Value = payload }, cancellationToken);
}