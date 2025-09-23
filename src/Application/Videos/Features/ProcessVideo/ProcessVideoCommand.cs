using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using Domain.Videos;
using Domain.Videos.Ports;
using MediatR;

namespace Application.Videos.UseCases.ProcessVideo;

public record ProcessVideoCommand(string VideoId, string? MessageType) : IRequest<ProcessVideoResponse?>;

public record ProcessVideoResponse(
    string VideoId,
    DateTimeOffset CompletedAt,
    double ProcessingTimeMs,
    IReadOnlyCollection<QrCodeResponse> QrCodes);

public record QrCodeResponse(string Text, string FormattedTimestamp);

public class ProcessVideoHandler(
    IQrCodeExtractor qrCodeExtractor,
    IVideoStatusRepository videoStatusRepository,
    IResultsPublisher resultsPublisher)
    : IRequestHandler<ProcessVideoCommand, ProcessVideoResponse?>
{
    public async Task<ProcessVideoResponse?> Handle(ProcessVideoCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[DEBUG] ProcessVideoHandler.Handle called: videoId={request.VideoId}, messageType={request.MessageType}");
        
        if (request.MessageType != "completed")
        {
            Console.WriteLine($"[DEBUG] Skipping non-completed message: {request.MessageType}");
            return null;
        }

        Console.WriteLine($"[DEBUG] Processing completed message for videoId={request.VideoId}");
        var videoId = VideoId.From(request.VideoId);

        // 1. Update status to Processing
        await videoStatusRepository.UpsertAsync(
            new UploadStatus(request.VideoId, UploadStage.Processing, -1, 0, 0, DateTime.UtcNow), 
            cancellationToken);

        var startTime = DateTimeOffset.UtcNow;
        
        try
        {
            // 2. Extract QR codes from video (encapsulates all technical complexity)
            var qrCodes = await qrCodeExtractor.ExtractFromVideoAsync(videoId, cancellationToken);

            // 3. Calculate metrics
            var processingTime = DateTimeOffset.UtcNow.Subtract(startTime).TotalMilliseconds;
            var processingMetrics = new ProcessingMetrics(
                startTime.DateTime, 
                DateTimeOffset.UtcNow.DateTime, 
                qrCodes.Count, 
                TimeSpan.FromMilliseconds(processingTime));

            // 4. Update status to Processed
            await videoStatusRepository.UpsertAsync(
                new UploadStatus(request.VideoId, UploadStage.Processed, -1, 0, 0, DateTime.UtcNow), 
                cancellationToken);

            // 5. Publish results
            var resultMessage = new
            {
                VideoId = request.VideoId,
                CompletedAt = DateTimeOffset.UtcNow,
                ProcessingTimeMs = processingTime,
                QrCodes = qrCodes.Select(qr => new
                {
                    Text = qr.Content,
                    TimestampSeconds = qr.DetectedAt.Seconds,
                    FormattedTimestamp = qr.FormattedTimestamp
                }).ToArray()
            };

            await resultsPublisher.PublishResultsAsync(request.VideoId, resultMessage, cancellationToken);

            // 6. Return response
            var response = new ProcessVideoResponse(
                VideoId: request.VideoId,
                CompletedAt: DateTimeOffset.UtcNow,
                ProcessingTimeMs: processingTime,
                QrCodes: qrCodes.Select(qr => 
                    new QrCodeResponse(qr.Content, qr.FormattedTimestamp)).ToList());

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Processing failed: {ex.Message}");
            throw;
        }
        finally
        {
            // Cleanup is handled internally by QrCodeExtractor
        }
    }
}