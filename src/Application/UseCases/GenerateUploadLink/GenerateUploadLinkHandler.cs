using Application.Ports;
using Domain.Common;
using Domain.Models;
using MediatR;

namespace Application.UseCases.GenerateUploadLink;

public class GenerateUploadLinkHandler(
    IUploadLinkGenerator uploadLinkGenerator, 
    IStatusWriteOnlyRepository statusWriteOnlyRepository) 
    : IRequestHandler<GenerateUploadLinkCommand, Result<GenerateUploadLinkResult>>
{
    public async Task<Result<GenerateUploadLinkResult>> Handle(GenerateUploadLinkCommand request, CancellationToken cancellationToken)
    {
        var videoId = !request.VideoId.HasValue ? VideoId.New() : new VideoId(request.VideoId.Value);

        var generateLinkResult = await uploadLinkGenerator.GenerateAsync(
            videoId.ToString(), cancellationToken);
        if (!generateLinkResult.IsSuccess)
            return Result<GenerateUploadLinkResult>.FromResult(generateLinkResult);
        
        var upsertResult = await statusWriteOnlyRepository.UpsertAsync(
            new Status(videoId.ToString(), Stage.Created), cancellationToken);
        if (!upsertResult.IsSuccess)
            return Result<GenerateUploadLinkResult>.FromResult(upsertResult);
        
        var link = generateLinkResult.Value!;
        
        return new GenerateUploadLinkResult(videoId.ToString(), link.Url, link.ExpiresAt);
    }
}