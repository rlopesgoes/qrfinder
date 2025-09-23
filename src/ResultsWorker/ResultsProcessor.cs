using System.Text.Json;
using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using Confluent.Kafka;
using Domain.Videos;

namespace ResultsWorker;

public class ResultsProcessor(
    IConsumer<string, byte[]> consumer,
    IVideoProcessingRepository repository,
    IVideoStatusRepository statusRepository) : BackgroundService
{
    private readonly string _topicResults = "videos.results";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe(_topicResults);
        Console.WriteLine($"[ResultsWorker] Subscribed to: {_topicResults}");

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, byte[]>? cr = null;
            try
            {
                cr = consumer.Consume(stoppingToken);
                if (cr is null) continue;

                var videoId = cr.Message.Key!;
                var resultData = JsonSerializer.Deserialize<VideoResultMessage>(cr.Message.Value)!;

                Console.WriteLine($"[ResultsWorker] Processing results for video: {videoId}");

                if (string.IsNullOrEmpty(resultData.VideoId))
                {
                    Console.WriteLine($"[ResultsWorker] Invalid video ID in message, skipping");
                    consumer.Commit(cr);
                    continue;
                }

                var qrCodes = (resultData.QrCodes ?? [])
                    .Where(c => !string.IsNullOrEmpty(c.Text))
                    .Select(c => new QrCodeResultDto(
                        c.Text!,
                        c.TimestampSeconds,
                        c.FormattedTimestamp ?? "",
                        DateTime.UtcNow))
                    .ToList();

                var videoProcessingResult = new VideoProcessingResult(
                    videoId,
                    "Completed",
                    resultData.CompletedAt.AddSeconds(-resultData.ProcessingTimeMs / 1000).DateTime,
                    resultData.CompletedAt.DateTime,
                    qrCodes.Count,
                    qrCodes);

                await repository.SaveAsync(videoProcessingResult, stoppingToken);

                Console.WriteLine($"[ResultsWorker] Saved {qrCodes.Count} QR codes for video: {videoId}");
                consumer.Commit(cr);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResultsWorker] Error processing message: {ex.Message}");
                Console.WriteLine($"[ResultsWorker] Exception details: {ex}");
                
                if (cr != null)
                {
                    try
                    {
                        var videoId = cr.Message.Key;
                        if (!string.IsNullOrEmpty(videoId))
                        {
                            await statusRepository.UpsertAsync(
                                new UploadStatus(videoId, VideoProcessingStage.Failed, -1, 0, 0, DateTime.UtcNow), 
                                stoppingToken);
                        }
                        
                        consumer.StoreOffset(cr);
                        consumer.Commit();
                        Console.WriteLine($"[ResultsWorker] Committed failed message to avoid reprocessing");
                    }
                    catch (Exception commitEx)
                    {
                        Console.WriteLine($"[ResultsWorker] Failed to commit after error: {commitEx.Message}");
                    }
                }
                
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}

public record VideoResultMessage(
    string? VideoId,
    DateTimeOffset CompletedAt,
    double ProcessingTimeMs,
    QrCodeMessageDto[]? QrCodes);

public record QrCodeMessageDto(
    string? Text,
    double TimestampSeconds,
    string? FormattedTimestamp);