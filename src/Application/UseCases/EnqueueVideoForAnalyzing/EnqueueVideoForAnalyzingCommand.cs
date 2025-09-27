using Domain.Common;
using MediatR;

namespace Application.UseCases;

public record EnqueueVideoForAnalyzingCommand(string VideoId) : IRequest<Result<EnqueueVideoForAnalyzingResult>>;