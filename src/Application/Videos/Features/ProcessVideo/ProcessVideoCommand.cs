using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
//using Application.Videos.Services;
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
    IAnalysisStatusRepository analysisStatusRepository,
    IResultsPublisher resultsPublisher)
    : IRequestHandler<ProcessVideoCommand, ProcessVideoResponse?>
{
    public async Task<ProcessVideoResponse?> Handle(ProcessVideoCommand request, CancellationToken cancellationToken)
    {

        var videoId = VideoId.From(request.VideoId);
        var currentStatus = await analysisStatusRepository.GetAsync(request.VideoId, cancellationToken);
        
    //    if (currentStatus?.Stage != VideoProcessingStage.Uploaded)
     //       throw new InvalidOperationException("Video must be uploaded before processing");

        await analysisStatusRepository.UpsertAsync(
            new ProcessStatus(request.VideoId, VideoProcessingStage.Processing), 
            cancellationToken);

        // Send processing started notification
      //  await progressService.StartProcessingAsync(request.VideoId, cancellationToken);

        var startTime = DateTimeOffset.UtcNow;
        
        try
        {
            var qrCodes = await qrCodeExtractor.ExtractFromVideoAsync(videoId, cancellationToken);
            
            var uniqueQrCodes = qrCodes
                .Where(qr => !string.IsNullOrWhiteSpace(qr.Content))
                .GroupBy(qr => qr.Content)
                .Select(group => group.OrderBy(qr => qr.DetectedAt.Seconds).First())
                .OrderBy(qr => qr.DetectedAt.Seconds)
                .ToList();

            var processingTime = DateTimeOffset.UtcNow.Subtract(startTime).TotalMilliseconds;

            await analysisStatusRepository.UpsertAsync(
                new ProcessStatus(request.VideoId, VideoProcessingStage.Processed), 
                cancellationToken);

            // Send processing completed notification
        //    await progressService.CompleteProcessingAsync(request.VideoId, uniqueQrCodes.Count, cancellationToken);

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

            return new ProcessVideoResponse(
                VideoId: request.VideoId,
                CompletedAt: DateTimeOffset.UtcNow,
                ProcessingTimeMs: processingTime,
                QrCodes: uniqueQrCodes.Select(qr => 
                    new QrCodeResponse(qr.Content, qr.FormattedTimestamp)).ToList());
        }
        catch (Exception ex)
        {
            await analysisStatusRepository.UpsertAsync(
                new ProcessStatus(request.VideoId, VideoProcessingStage.Failed), 
                cancellationToken);

            // Send processing failed notification
       //     await progressService.FailAsync(request.VideoId, ex.Message, cancellationToken);
            
            throw;
        }
    }
}