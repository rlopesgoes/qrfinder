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
    IVideoContentService contentService,
    INotificationService notificationService,
    IVideoStatusRepository videoStatusRepository)
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

        // Original logic: Process file directly without aggregate
        Console.WriteLine($"[DEBUG] Processing video file directly...");

        // 3. Finalize video file (technical operation)
        var videoPath = await contentService.FinalizeVideoAsync(videoId, cancellationToken);

        var startTime = DateTimeOffset.UtcNow;
        
        try
        {
            // 4. Detect QR codes (preserving ALL original fallback logic)
            var detections = await contentService.DetectQrCodesAsync(videoPath, cancellationToken);

            // 5. Convert detections to QR codes (domain logic)
            var qrCodes = detections.Select(d => QrCode.Create(d.Text, d.TimestampSeconds)).ToList();

            // 6. Create processing result
            var processingTime = DateTimeOffset.UtcNow.Subtract(startTime).TotalMilliseconds;
            var processingMetrics = new ProcessingMetrics(
                startTime.DateTime, 
                DateTimeOffset.UtcNow.DateTime, 
                detections.Count, 
                TimeSpan.FromMilliseconds(processingTime));
            
            var processingResult = new ProcessingResult(videoId, qrCodes, processingMetrics);

            // 7. Update status to Processed
            await videoStatusRepository.UpsertAsync(
                new UploadStatus(request.VideoId, UploadStage.Processed, -1, 0, 0, DateTime.UtcNow), 
                cancellationToken);

            // 8. Notify completion
            await notificationService.NotifyProcessingCompletedAsync(videoId, processingResult, cancellationToken);

            // 9. Return response (preserving exact original JSON structure)
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
            // 9. Cleanup (preserving original logic)
            await contentService.CleanupVideoAsync(videoPath, cancellationToken);
        }
    }
}