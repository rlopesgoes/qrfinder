using System.Text.Json;
using Application;
using Application.UseCases.SaveAnalysisResults;
using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using Confluent.Kafka;
using Domain.Videos;
using MediatR;

namespace ResultsWorker;

public class ResultsProcessor(
    IConsumer<string, byte[]> consumer,
    IVideoProcessingRepository repository,
    IMediator mediator,
    IAnalysisStatusRepository statusRepository) : BackgroundService
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
                var result = await mediator.Send(new SaveAnalysisResultsCommand(
                    resultData), stoppingToken);
                
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
                                new ProcessStatus(videoId, VideoProcessingStage.Failed, -1, 0, 0, DateTime.UtcNow), 
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