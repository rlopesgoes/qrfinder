using System.Text.Json;
using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using Confluent.Kafka;

namespace ResultsWorker;

public class ResultsProcessor(
    IConsumer<string, byte[]> consumer,
    IVideoProcessingRepository repository) : BackgroundService
{
    private readonly string _topicResults = "videos.results";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe(_topicResults);
        Console.WriteLine($"[ResultsWorker] Subscribed to: {_topicResults}");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(stoppingToken);
                if (cr is null) continue;

                var videoId = cr.Message.Key!;
                var resultData = JsonSerializer.Deserialize<VideoResultMessage>(cr.Message.Value)!;

                Console.WriteLine($"[ResultsWorker] Processing results for video: {videoId}");

                var qrCodes = resultData.QrCodes.Select(c => new QrCodeResultDto(
                    c.Text,
                    c.TimestampSeconds,
                    c.FormattedTimestamp,
                    DateTime.UtcNow)).ToList();

                var videoProcessingResult = new VideoProcessingResult(
                    videoId,
                    "Completed",
                    resultData.CompletedAt.AddSeconds(-resultData.ProcessingTimeMs / 1000).DateTime,
                    resultData.CompletedAt.DateTime,
                    qrCodes.Count,
                    qrCodes);

                await repository.SaveAsync(videoProcessingResult, stoppingToken);

                Console.WriteLine($"[ResultsWorker] Saved {qrCodes.Count} QR codes for video: {videoId}");
                consumer.StoreOffset(cr);
                consumer.Commit();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResultsWorker] Error: {ex.Message}");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}

public record VideoResultMessage(
    string VideoId,
    DateTimeOffset CompletedAt,
    double ProcessingTimeMs,
    QrCodeMessageDto[] QrCodes);

public record QrCodeMessageDto(
    string Text,
    double TimestampSeconds,
    string FormattedTimestamp);