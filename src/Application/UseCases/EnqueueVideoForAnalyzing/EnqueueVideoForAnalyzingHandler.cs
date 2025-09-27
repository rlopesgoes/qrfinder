using Application.Videos.Ports;
using Domain.Common;
using MediatR;
using VideoProcessingStage = Domain.Videos.VideoProcessingStage;

namespace Application.UseCases.EnqueueVideoForAnalyzing;

public class EnqueueVideoForAnalyzingHandler(
    IAnalysisStatusRepository analysisStatusRepository,
    IVideoAnalysisQueue videoAnalysisQueue,
    IAnalyzeProgressNotifier progressNotifier) 
    : IRequestHandler<EnqueueVideoForAnalyzingCommand, Result<EnqueueVideoForAnalyzingResult>>
{
    public async Task<Result<EnqueueVideoForAnalyzingResult>> Handle(EnqueueVideoForAnalyzingCommand command, CancellationToken cancellationToken)
    {
        var statusResult = await analysisStatusRepository.GetAsync(command.VideoId, cancellationToken);
        if (!statusResult.IsSuccess)
            return Result<EnqueueVideoForAnalyzingResult>.FromResult(statusResult);
        var status = statusResult.Value!;
        
        if (status.Stage is not VideoProcessingStage.Created)
            return Result<EnqueueVideoForAnalyzingResult>.WithError($"Video {command.VideoId} is already being processed");
        
        var enqueueResult = await videoAnalysisQueue.EnqueueAsync(command.VideoId, cancellationToken);
        if (!enqueueResult.IsSuccess)
            return Result<EnqueueVideoForAnalyzingResult>.FromResult(enqueueResult);
        
        var enqueuedAt = DateTime.UtcNow;
        
        var upsertResult = await analysisStatusRepository.UpsertAsync(new (command.VideoId, VideoProcessingStage.Sent), cancellationToken);
        if (!upsertResult.IsSuccess)
            return Result<EnqueueVideoForAnalyzingResult>.FromResult(upsertResult);
        
        var notifyProgressResult = await progressNotifier.NotifyProgressAsync(new AnalyzeProgressNotification(command.VideoId, Stage: nameof(VideoProcessingStage.Sent)), cancellationToken);
        if (!notifyProgressResult.IsSuccess)
            return Result<EnqueueVideoForAnalyzingResult>.FromResult(notifyProgressResult);
        
        return new EnqueueVideoForAnalyzingResult(command.VideoId, enqueuedAt);
    }
}