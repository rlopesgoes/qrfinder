using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using Domain.Common;
using Domain.Videos;
using Domain.Videos.Ports;
using MediatR;

namespace Application.UseCases.ScanQrCode;

public class ScanQrCodeHandler(
    IQrCodeExtractor qrCodeExtractor,
    IAnalysisStatusRepository analysisStatusRepository,
    IAnalyzeProgressNotifier analyzeProgressNotifier,
    IResultsPublisher resultsPublisher)
    : IRequestHandler<ScanQrCodeCommand, Result<ScanQrCodeResponse>>
{
    public async Task<Result<ScanQrCodeResponse>> Handle(ScanQrCodeCommand request, CancellationToken cancellationToken)
    {
        var videoId = VideoId.From(request.VideoId);
        
        var currentStatusResult = await analysisStatusRepository.GetAsync(request.VideoId, cancellationToken);
        if (!currentStatusResult.IsSuccess)
            return Result<ScanQrCodeResponse>.FromResult(currentStatusResult);
        var currentStatus = currentStatusResult.Value!;

        if (currentStatus.Stage is not VideoProcessingStage.Sent)
            return Result<ScanQrCodeResponse>.WithError($"Video {request.VideoId} is not in the sent state");

        await analysisStatusRepository.UpsertAsync(new ProcessStatus(request.VideoId, VideoProcessingStage.Processing), cancellationToken);
        await analyzeProgressNotifier.NotifyProgressAsync(new AnalyzeProgressNotification(request.VideoId, nameof(VideoProcessingStage.Processing), 50), cancellationToken);

        var startTime = DateTimeOffset.UtcNow;
        
        var qrCodesResult = await qrCodeExtractor.ExtractFromVideoAsync(videoId, cancellationToken);
        if (!qrCodesResult.IsSuccess)
        {
            await analysisStatusRepository.UpsertAsync(new ProcessStatus(request.VideoId, VideoProcessingStage.Failed), cancellationToken);
            await analyzeProgressNotifier.NotifyProgressAsync(new AnalyzeProgressNotification(request.VideoId, nameof(VideoProcessingStage.Failed), Message: qrCodesResult.Error?.Message), cancellationToken);
            return Result<ScanQrCodeResponse>.FromResult(qrCodesResult);
        }
        var qrCodes = qrCodesResult.Value!;
        
        var processingTime = DateTimeOffset.UtcNow.Subtract(startTime).TotalMilliseconds;
        
        var uniqueQrCodes = qrCodes.Values
            .Where(qr => !string.IsNullOrWhiteSpace(qr.Content))
            .GroupBy(qr => qr.Content)
            .Select(group => group.OrderBy(qr => qr.DetectedAt.Seconds).First())
            .OrderBy(qr => qr.DetectedAt.Seconds)
            .ToList();

        await analysisStatusRepository.UpsertAsync(new ProcessStatus(request.VideoId, VideoProcessingStage.Processed), cancellationToken);
        await analyzeProgressNotifier.NotifyProgressAsync(new AnalyzeProgressNotification(request.VideoId, nameof(VideoProcessingStage.Processed), ProgressPercentage: 100), cancellationToken);

        var resultMessage = new
        {
            VideoId = request.VideoId,
            CompletedAt = DateTimeOffset.UtcNow,
            ProcessingTimeMs = processingTime,
            QrCodes = uniqueQrCodes.Select(qr => new
            {
                Text = qr.Content,
                TimestampSeconds = qr.DetectedAt.Seconds,
                FormattedTimestamp = qr.FormattedTimestamp
            }).ToArray()
        };

        await resultsPublisher.PublishResultsAsync(request.VideoId, resultMessage, cancellationToken);

        return new ScanQrCodeResponse(
            VideoId: request.VideoId,
            CompletedAt: DateTimeOffset.UtcNow,
            ProcessingTimeMs: processingTime,
            QrCodes: uniqueQrCodes.Select(qr => 
                new QrCodeResponse(qr.Content, qr.FormattedTimestamp)).ToList());
    }
}