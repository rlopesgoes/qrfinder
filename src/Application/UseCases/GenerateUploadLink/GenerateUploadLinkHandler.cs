
using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using Domain.Common;
using Domain.Videos;
using MediatR;
using VideoProcessingStage = Domain.Videos.VideoProcessingStage;

namespace Application.UseCases.GenerateUploadLink;

public class GenerateUploadLinkHandler(
    IUploadLinkGenerator uploadLinkGenerator, 
    IAnalysisStatusRepository analysisStatusRepository) 
    : IRequestHandler<GenerateUploadLinkCommand, Result<GenerateUploadLinkResult>>
{
    public async Task<Result<GenerateUploadLinkResult>> Handle(GenerateUploadLinkCommand request, CancellationToken cancellationToken)
    {
        var videoId = !request.VideoId.HasValue ? VideoId.New() : new VideoId(request.VideoId.Value);

        var generateLinkResult = await uploadLinkGenerator.GenerateAsync(
            videoId.ToString(), cancellationToken);
        if (!generateLinkResult.IsSuccess)
            return Result<GenerateUploadLinkResult>.FromResult(generateLinkResult);
        
        var upsertResult = await analysisStatusRepository.UpsertAsync(
            new ProcessStatus(videoId.ToString(), VideoProcessingStage.Created), cancellationToken);
        if (!upsertResult.IsSuccess)
            return Result<GenerateUploadLinkResult>.FromResult(upsertResult);
        
        var link = generateLinkResult.Value!;
        
        return new GenerateUploadLinkResult(videoId.ToString(), link.Url, link.ExpiresAt);
    }
}