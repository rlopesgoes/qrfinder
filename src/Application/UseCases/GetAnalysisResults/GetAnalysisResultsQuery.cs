using Domain.Common;
using MediatR;

namespace Application.UseCases.GetAnalysisResults;

public record GetAnalysisResultsQuery(string VideoId) : IRequest<Result<GetAnalysisResultsResult>>;