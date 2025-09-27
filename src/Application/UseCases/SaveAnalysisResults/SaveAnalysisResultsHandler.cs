using Application.Videos.Features.GetVideoResults;
using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using Domain.Common;
using MediatR;

namespace Application.UseCases.SaveAnalysisResults;

public class SaveAnalysisResultsHandler(IVideoProcessingRepository repository) : IRequestHandler<SaveAnalysisResultsCommand, Result<SaveAnalysisResultsResponse>>
{
    public async Task<Result<SaveAnalysisResultsResponse>> Handle(SaveAnalysisResultsCommand request, CancellationToken cancellationToken)
    {
        var videoId = request.Message.VideoId;

        var qrCodes = (request.Message.QrCodes ?? [])
            .Where(c => !string.IsNullOrEmpty(c.Text))
            .Select(c => new QrCodeResult(
                c.Text!,
                c.TimestampSeconds,
                c.FormattedTimestamp ?? "",
                DateTime.UtcNow))
            .ToList();

        var videoProcessingResult = new VideoProcessingResult(
            videoId,
            "Completed",
            request.Message.CompletedAt.AddSeconds(-request.Message.ProcessingTimeMs / 1000).DateTime,
            request.Message.CompletedAt.DateTime,
            qrCodes.Count,
            qrCodes);

        await repository.SaveAsync(videoProcessingResult, cancellationToken);

        return new Result<SaveAnalysisResultsResponse>();
    }
}