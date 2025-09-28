using Application.Ports;
using Domain.Common;
using Domain.Models;
using MediatR;

namespace Application.UseCases.ScanQrCode;

public class ScanQrCodeHandler(
    IQrCodeExtractor qrCodeExtractor,
    IStatusReadOnlyRepository statusReadOnlyRepository,
    IStatusWriteOnlyRepository statusWriteOnlyRepository,
    IProgressNotifier progressNotifier,
    IResultsPublisher resultsPublisher,
    IVideosWriteOnlyRepository videosWriteOnlyRepository)
    : IRequestHandler<ScanQrCodeCommand, Result<ScanQrCodeResult>>
{
    public async Task<Result<ScanQrCodeResult>> Handle(ScanQrCodeCommand command, CancellationToken cancellationToken)
    {
        var videoId = VideoId.From(command.VideoId);
        
        var currentStatusResult = await statusReadOnlyRepository.GetAsync(videoId.ToString(), cancellationToken);
        if (!currentStatusResult.IsSuccess)
            return Result<ScanQrCodeResult>.FromResult(currentStatusResult);
        var currentStatus = currentStatusResult.Value!;

        if (currentStatus.Stage is not Stage.Sent)
            return Result<ScanQrCodeResult>.WithError($"Video {videoId.ToString()} is not in the sent state");

        await statusWriteOnlyRepository.UpsertAsync(new Status(videoId.ToString(), Stage.Processing), cancellationToken);
        await progressNotifier.NotifyAsync(new ProgressNotification(videoId.ToString(), nameof(Stage.Processing), 50), cancellationToken);

        var startTime = DateTimeOffset.UtcNow;
        
        var qrCodesResult = await qrCodeExtractor.ExtractFromVideoAsync(videoId, cancellationToken);
        if (!qrCodesResult.IsSuccess)
        {
            await statusWriteOnlyRepository.UpsertAsync(new Status(videoId.ToString(), Stage.Failed), cancellationToken);
            await progressNotifier.NotifyAsync(new ProgressNotification(videoId.ToString(), nameof(Stage.Failed), Message: qrCodesResult.Error?.Message), cancellationToken);
            return Result<ScanQrCodeResult>.FromResult(qrCodesResult);
        }
        var qrCodes = qrCodesResult.Value!;
        
        var processingTime = DateTimeOffset.UtcNow.Subtract(startTime).TotalMilliseconds;
        
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
            CompletedAt = DateTimeOffset.UtcNow,
            ProcessingTimeMs = processingTime,
            QrCodes = uniqueQrCodes.Select(qr => new
            {
                Text = qr.Content,
                TimestampSeconds = qr.TimeStamp.Seconds,
                FormattedTimestamp = qr.FormattedTimestamp
            }).ToList()
        };

        await resultsPublisher.PublishResultsAsync(videoId.ToString(), resultMessage, cancellationToken);

        await videosWriteOnlyRepository.DeleteAsync(videoId.ToString(), cancellationToken);
        
        return new ScanQrCodeResult(
            VideoId: command.VideoId,
            CompletedAt: DateTimeOffset.UtcNow,
            ProcessingTimeMs: processingTime,
            QrCodes: uniqueQrCodes.Select(qr => 
                new QrCodeResponse(qr.Content, qr.FormattedTimestamp)).ToList());
    }
}