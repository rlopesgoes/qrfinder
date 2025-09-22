using MediatR;

namespace Application.Videos.Features.GetVideoResults;

public record GetVideoResultsRequest(string VideoId) : IRequest<GetVideoResultsResponse>;