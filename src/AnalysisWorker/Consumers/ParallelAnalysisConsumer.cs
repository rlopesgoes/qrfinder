using System.Text.Json;
using System.Threading.Channels;
using Application.UseCases.ScanQrCode;
using Confluent.Kafka;
using MediatR;

namespace Worker;

public class ParallelAnalysisConsumer(
    IConsumer<string, string> consumer,
    IMediator mediator,
    ILogger<ParallelAnalysisConsumer> logger,
    IConfiguration configuration) : BackgroundService
{
    private const string TopicControl = "video.analysis.queue";
    private readonly int _maxConcurrency = configuration.GetValue<int>("AnalysisWorker:MaxConcurrency", 3);
    private readonly SemaphoreSlim _semaphore = new(configuration.GetValue<int>("AnalysisWorker:MaxConcurrency", 3));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe(TopicControl);
        logger.LogInformation("Starting parallel analysis consumer with max concurrency: {MaxConcurrency}", _maxConcurrency);

        var processingTasks = new List<Task>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (consumeResult is null)
                {
                    // Limpa tasks completadas
                    processingTasks.RemoveAll(t => t.IsCompleted);
                    continue;
                }

                var message = JsonSerializer.Deserialize<VideoAnalysisMessage>(consumeResult.Message.Value);
                if (message is null)
                {
                    consumer.Commit(consumeResult);
                    continue;
                }

                // Processa mensagem em paralelo
                var task = ProcessMessageAsync(consumeResult, message, stoppingToken);
                processingTasks.Add(task);

                // Limpa tasks completadas
                processingTasks.RemoveAll(t => t.IsCompleted);
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Error consuming from Kafka");
            }
        }

        // Aguarda todas as tasks completarem antes de finalizar
        await Task.WhenAll(processingTasks);
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult, VideoAnalysisMessage message, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        
        try
        {
            logger.LogInformation("Processing video {VideoId} (Thread: {ThreadId})", message.VideoId, Thread.CurrentThread.ManagedThreadId);

            // Read start time from header
            DateTime? startTime = null;
            var startTimeHeader = consumeResult.Message.Headers?.FirstOrDefault(h => h.Key == "x-started-at");
            if (startTimeHeader?.GetValueBytes() != null)
            {
                var startTimeString = System.Text.Encoding.UTF8.GetString(startTimeHeader.GetValueBytes());
                if (DateTime.TryParseExact(startTimeString, "O", null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedStartTime))
                    startTime = parsedStartTime;
            }

            var result = await mediator.Send(new ScanQrCodeCommand(message.VideoId, startTime), cancellationToken);

            // Commit após processamento bem-sucedido
            lock (consumer)
            {
                consumer.Commit(consumeResult);
            }

            if (!result.IsSuccess)
                logger.LogError("Failed to process video {VideoId}: {Error}", message.VideoId, result.Error?.Message);
            else
                logger.LogInformation("Successfully processed video {VideoId} (Thread: {ThreadId})", message.VideoId, Thread.CurrentThread.ManagedThreadId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing video {VideoId}", message.VideoId);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}