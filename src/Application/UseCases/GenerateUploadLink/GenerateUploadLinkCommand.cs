using Domain.Common;
using MediatR;

namespace Application.UseCases.GenerateUploadLink;

public record GenerateUploadLinkCommand(Guid? VideoId = null) : 
    IRequest<Result<GenerateUploadLinkResult>>;