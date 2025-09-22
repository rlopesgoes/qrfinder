using MediatR;

namespace Application.Videos.Features.RebuildVideo;

public record RebuildVideoRequest(string VideoId, byte[] VideoPart) : IRequest;