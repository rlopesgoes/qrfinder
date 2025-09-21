using MediatR;

namespace Application.Videos.GetVideoResults;

public record GetVideoResultsRequest(string VideoId) : IRequest<GetVideoResultsResponse>;