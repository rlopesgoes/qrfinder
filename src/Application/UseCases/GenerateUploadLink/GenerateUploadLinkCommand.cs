using Domain.Common;
using MediatR;

namespace Application.UseCases;

public record GenerateUploadLinkCommand(Guid? VideoId = null) : 
    IRequest<Result<GenerateUploadLinkResult>>;