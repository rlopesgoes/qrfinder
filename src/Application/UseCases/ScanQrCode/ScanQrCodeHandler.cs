using Application.Ports;
using Domain.Common;
using Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.ScanQrCode;

public class ScanQrCodeHandler(
    ILogger<ScanQrCodeHandler> logger,
    IQrCodeScanner qrCodeScanner,
    IStatusReadOnlyRepository statusReadOnlyRepository,
    IStatusWriteOnlyRepository statusWriteOnlyRepository,
    IProgressNotifier progressNotifier,
    IResultsPublisher resultsPublisher,
    IVideosWriteOnlyRepository videosWriteOnlyRepository,
    IVideosReadOnlyRepository videosReadOnlyRepository)
    : IRequestHandler<ScanQrCodeCommand, Result<ScanQrCodeResult>>
{
    public async Task<Result<ScanQrCodeResult>> Handle(ScanQrCodeCommand command, CancellationToken cancellationToken)
    {
        var videoId = VideoId.From(command.VideoId);
        
        logger.LogInformation("Scanning QR codes for video {VideoId}", videoId.ToString());
        
        var currentStatusResult = await statusReadOnlyRepository.GetAsync(videoId.ToString(), cancellationToken);
        if (!currentStatusResult.IsSuccess)
            return Result<ScanQrCodeResult>.FromResult(currentStatusResult);
        var currentStatus = currentStatusResult.Value!;

        if (currentStatus.Stage is not Stage.Sent)
            return Result<ScanQrCodeResult>.WithError($"Video {videoId.ToString()} is not in the sent state");
        
        logger.LogInformation("Video {VideoId} is in the sent state", videoId.ToString());

        await statusWriteOnlyRepository.UpsertAsync(new Status(videoId.ToString(), Stage.Processing), cancellationToken);
        await progressNotifier.NotifyAsync(new ProgressNotification(videoId.ToString(), nameof(Stage.Processing), 50), cancellationToken);

        var startTime = command.StartedAt.HasValue 
            ? new DateTimeOffset(command.StartedAt.Value, TimeSpan.Zero)
            : DateTimeOffset.UtcNow;
        
        var videoResult = await videosReadOnlyRepository.GetAsync(videoId.ToString(), cancellationToken);
        if (!videoResult.IsSuccess)
        {
            await statusWriteOnlyRepository.UpsertAsync(new Status(videoId.ToString(), Stage.Failed), cancellationToken);
            await progressNotifier.NotifyAsync(new ProgressNotification(videoId.ToString(), nameof(Stage.Failed), Message: videoResult.Error?.Message), cancellationToken);
            return Result<ScanQrCodeResult>.FromResult(videoResult);
        }
        var video = videoResult.Value!;
        
        logger.LogInformation("Video {VideoId} is in the processing state", videoId.ToString());
        
        var qrCodesResult = await qrCodeScanner.ScanAsync(video, cancellationToken);
        if (!qrCodesResult.IsSuccess)
        {
            await statusWriteOnlyRepository.UpsertAsync(new Status(videoId.ToString(), Stage.Failed), cancellationToken);
            await progressNotifier.NotifyAsync(new ProgressNotification(videoId.ToString(), nameof(Stage.Failed), Message: qrCodesResult.Error?.Message), cancellationToken);
            return Result<ScanQrCodeResult>.FromResult(qrCodesResult);
        }
        var qrCodes = qrCodesResult.Value!;
        
        logger.LogInformation("Video {VideoId} is in the processed state", videoId.ToString());
        
        var endTime = DateTimeOffset.UtcNow;
        var processingTime = endTime.Subtract(startTime).TotalMilliseconds;
        var totalProcessingTime = endTime.Subtract(startTime).TotalSeconds;
        
        var uniqueQrCodes = qrCodes.Values
            .Where(qr => !string.IsNullOrWhiteSpace(qr.Content))
            .GroupBy(qr => qr.Content)
            .Select(group => group.OrderBy(qr => qr.TimeStamp.Seconds).First())
            .OrderBy(qr => qr.TimeStamp.Seconds)
            .ToList();

        await statusWriteOnlyRepository.UpsertAsync(new Status(videoId.ToString(), Stage.Processed), cancellationToken);
        await progressNotifier.NotifyAsync(new ProgressNotification(videoId.ToString(), nameof(Stage.Processed), ProgressPercentage: 100), cancellationToken);

        var resultMessage = new
        {
            VideoId = videoId.ToString(),
            CompletedAt = endTime,
            ProcessingTimeMs = processingTime,
            TotalProcessingTimeSeconds = totalProcessingTime,
            QrCodes = uniqueQrCodes.Select(qr => new
            {
                Text = qr.Content,
                TimestampSeconds = qr.TimeStamp.Seconds,
                FormattedTimestamp = qr.FormattedTimestamp
            }).ToList()
        };

        await resultsPublisher.PublishResultsAsync(videoId.ToString(), resultMessage, cancellationToken);

        await videosWriteOnlyRepository.DeleteAsync(videoId.ToString(), cancellationToken);
        
        logger.LogInformation("Video {VideoId} is in the deleted state", videoId.ToString());
        
        return new ScanQrCodeResult(
            VideoId: command.VideoId,
            CompletedAt: DateTimeOffset.UtcNow,
            ProcessingTimeMs: processingTime,
            QrCodes: uniqueQrCodes.Select(qr => 
                new QrCodeResponse(qr.Content, qr.FormattedTimestamp)).ToList());
    }
}