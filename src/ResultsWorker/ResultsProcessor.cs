using System.Text.Json;
using Application.Videos.Ports;
using Confluent.Kafka;
using Domain.Videos;

namespace ResultsWorker;

public class ResultsProcessor(
    IConsumer<string, byte[]> consumer,
    IVideoProcessingRepository repository) : BackgroundService
{
    private readonly string topicResults = "videos.results";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe(topicResults);
        Console.WriteLine($"[ResultsWorker] Subscribed to: {topicResults}");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(stoppingToken);
                if (cr is null) continue;

                var videoId = cr.Message.Key!;
                var resultData = JsonSerializer.Deserialize<VideoResultMessage>(cr.Message.Value)!;

                Console.WriteLine($"[ResultsWorker] Processing results for video: {videoId}");

                // Converte para domain objects
                var qrCodes = resultData.codes.Select(c => new QRCodeResult
                {
                    Text = c.text,
                    TimestampSeconds = c.timestampSeconds,
                    FormattedTime = c.formattedTime,
                    DetectedAt = DateTime.UtcNow
                }).ToList();

                // Verifica se já existe registro
                var existing = await repository.GetByVideoIdAsync(videoId, stoppingToken);
                if (existing == null)
                {
                    // Cria novo registro
                    var videoProcessing = new VideoProcessing
                    {
                        Id = Guid.NewGuid().ToString(),
                        VideoId = videoId,
                        Status = VideoProcessingStatus.Completed,
                        StartedAt = resultData.completedAt.AddSeconds(-10).DateTime, // Aproximação
                        CompletedAt = resultData.completedAt.DateTime,
                        TotalFramesProcessed = qrCodes.Count,
                        QRCodes = qrCodes
                    };

                    await repository.SaveAsync(videoProcessing, stoppingToken);
                }
                else
                {
                    // Atualiza com resultados
                    await repository.AddQRCodeResultsAsync(videoId, qrCodes, stoppingToken);
                }

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

// DTOs para deserialização
public record VideoResultMessage(
    string videoId,
    DateTimeOffset completedAt,
    double processingTimeMs,
    string totalFramesProcessed,
    QRCodeDto[] codes);

public record QRCodeDto(
    string text,
    double timestampSeconds,
    string formattedTime);