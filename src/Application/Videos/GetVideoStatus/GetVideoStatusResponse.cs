namespace Application.Videos.GetVideoStatus;

public record GetVideoStatusResponse(
    string VideoId,
    string Status);