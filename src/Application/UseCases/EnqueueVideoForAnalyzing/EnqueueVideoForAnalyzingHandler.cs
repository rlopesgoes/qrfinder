using Application.Ports;
using Domain.Common;
using Domain.Models;
using MediatR;

namespace Application.UseCases.EnqueueVideoForAnalyzing;

public class EnqueueVideoForAnalyzingHandler(
    IStatusReadOnlyRepository statusReadOnlyRepository,
    IStatusWriteOnlyRepository statusWriteOnlyRepository,
    IVideoAnalysisQueue videoAnalysisQueue,
    IProgressNotifier progressNotifier) 
    : IRequestHandler<EnqueueVideoForAnalyzingCommand, Result<EnqueueVideoForAnalyzingResult>>
{
    public async Task<Result<EnqueueVideoForAnalyzingResult>> Handle(EnqueueVideoForAnalyzingCommand command, CancellationToken cancellationToken)
    {
        var statusResult = await statusReadOnlyRepository.GetAsync(command.VideoId, cancellationToken);
        if (!statusResult.IsSuccess)
            return Result<EnqueueVideoForAnalyzingResult>.FromResult(statusResult);
        var status = statusResult.Value!;
        
        if (status.Stage is not Stage.Created)
            return Result<EnqueueVideoForAnalyzingResult>.WithError($"Video {command.VideoId} is already being processed");
        
        var enqueuedAt = DateTime.UtcNow;
        
        var enqueueResult = await videoAnalysisQueue.EnqueueAsync(command.VideoId, cancellationToken);
        if (!enqueueResult.IsSuccess)
            return Result<EnqueueVideoForAnalyzingResult>.FromResult(enqueueResult);
        
        var upsertResult = await statusWriteOnlyRepository.UpsertAsync(new (command.VideoId, Stage.Sent), cancellationToken);
        if (!upsertResult.IsSuccess)
            return Result<EnqueueVideoForAnalyzingResult>.FromResult(upsertResult);
        
        var notifyProgressResult = await progressNotifier.NotifyAsync(new ProgressNotification(command.VideoId, Stage: nameof(Stage.Sent)), cancellationToken);
        if (!notifyProgressResult.IsSuccess)
            return Result<EnqueueVideoForAnalyzingResult>.FromResult(notifyProgressResult);
        
        return new EnqueueVideoForAnalyzingResult(command.VideoId, enqueuedAt);
    }
}