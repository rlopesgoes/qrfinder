namespace Application.Videos.Features.GetVideoStatus;

public record GetVideoStatusResponse(
    string VideoId,
    string Status);