using Domain.Common;
using MediatR;

namespace Application.Videos.Features.GetVideoResults;

public record GetVideoResultsQuery(string VideoId) : IRequest<Result<GetVideoResultsResult>>;