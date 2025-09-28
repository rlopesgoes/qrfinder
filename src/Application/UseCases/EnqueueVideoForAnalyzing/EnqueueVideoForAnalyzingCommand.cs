using Domain.Common;
using MediatR;

namespace Application.UseCases.EnqueueVideoForAnalyzing;

public record EnqueueVideoForAnalyzingCommand(string VideoId) 
    : IRequest<Result<EnqueueVideoForAnalyzingResult>>;