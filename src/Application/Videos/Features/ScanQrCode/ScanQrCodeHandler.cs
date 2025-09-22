using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using Domain.Videos;
using MediatR;

namespace Application.Videos.Features.ScanQrCode;

public class ScanQrCodeHandler(
    IVideoStatusRepository videoStatusRepository,
    IVideoFileManager videoFileManager,
    IQrCodeDetector qrCodeDetector) 
    : IRequestHandler<ScanQrCodeRequest, ProcessVideoResult?>
{
    public async Task<ProcessVideoResult?> Handle(ScanQrCodeRequest request, CancellationToken cancellationToken)
    {
        if (request.MessageType != "completed")
            return null;

        var startTime = DateTimeOffset.UtcNow;
        
        // 1. Update status to processing
        await videoStatusRepository.UpsertAsync(
            new UploadStatus(request.VideoId, UploadStage.Processing), 
            cancellationToken);
        
        var videoPath = videoFileManager.FinalizeVideo(request.VideoId);
        
        try
        {
            var qrCodes = await qrCodeDetector.DetectQrCodesAsync(videoPath, cancellationToken);
            
            await videoStatusRepository.UpsertAsync(
                new UploadStatus(request.VideoId, UploadStage.Processed), 
                cancellationToken);
            
            var processingTime = DateTimeOffset.UtcNow.Subtract(startTime).TotalMilliseconds;
            return new ProcessVideoResult(
                VideoId: request.VideoId,
                CompletedAt: DateTimeOffset.UtcNow,
                ProcessingTimeMs: processingTime,
                QrCodes: qrCodes.Select(qr => new QrCodeResult(qr.Text, qr.FormattedTimestamp)).ToList());
        }
        finally
        {
            videoFileManager.CleanupVideo(videoPath);
        }
    }
}