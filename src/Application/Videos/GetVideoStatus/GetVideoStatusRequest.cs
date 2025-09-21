using MediatR;

namespace Application.Videos.GetVideoStatus;

public record GetVideoStatusRequest(string VideoId) : IRequest<GetVideoStatusResponse>;