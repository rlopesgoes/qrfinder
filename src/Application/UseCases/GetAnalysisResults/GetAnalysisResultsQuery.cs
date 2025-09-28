using Domain.Common;
using MediatR;

namespace Application.Videos.Features.GetVideoResults;

public record GetAnalysisResultsQuery(string VideoId) : IRequest<Result<GetAnalysisResultsResult>>;