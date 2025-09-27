using Domain.Common;
using MediatR;

namespace Application.UseCases.SaveAnalysisResults;

public record SaveAnalysisResultsCommand(VideoResultMessage Message) : IRequest<Result<SaveAnalysisResultsResponse>>;