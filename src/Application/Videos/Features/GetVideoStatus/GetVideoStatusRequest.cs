using MediatR;

namespace Application.Videos.Features.GetVideoStatus;

public record GetVideoStatusRequest(string VideoId) : IRequest<GetVideoStatusResponse?>;