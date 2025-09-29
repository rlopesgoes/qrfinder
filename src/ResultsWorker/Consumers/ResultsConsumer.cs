using System.Text.Json;
using Application.UseCases.SaveAnalysisResults;
using Confluent.Kafka;
using Contracts.Contracts.SaveAnalysisResults;
using Domain.Models;
using MediatR;
using Timestamp = Domain.Models.Timestamp;

namespace ResultsWorker.Consumers;

public class ResultsConsumer(
    ILogger<ResultsConsumer> logger,
    IConsumer<string, byte[]> consumer,
    IMediator mediator) : BackgroundService
{
    private const string TopicResults = "videos.results";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting ResultsConsumer");
        
        consumer.Subscribe(TopicResults);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult is null)
                    continue;

                var videoResultMessage = JsonSerializer.Deserialize<VideoResultMessage>(consumeResult.Message.Value);
                if (videoResultMessage is null)
                    continue;
                
                logger.LogInformation("Processing video {VideoId}", videoResultMessage.VideoId);

                var command = new SaveAnalysisResultsCommand(
                    videoResultMessage.VideoId,
                    videoResultMessage.CompletedAt,
                    videoResultMessage.ProcessingTimeMs,
                    new(videoResultMessage.QrCodes.Select(x => new QrCode(x.Text, new Timestamp(x.TimestampSeconds)))
                        .ToList()));

                var result = await mediator.Send(command, stoppingToken);
                if (!result.IsSuccess)
                    logger.LogError("Failed to process video {VideoId}: {Error}", videoResultMessage.VideoId,
                        result.Error?.Message);

                consumer.Commit(consumeResult);
                
                logger.LogInformation("Video {VideoId} processed", videoResultMessage.VideoId);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing Kafka message");
            }
        }
    }
}