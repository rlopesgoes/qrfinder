namespace Application.Videos.GetVideoStatus;

public record GetVideoStatusResponse(
    string VideoId,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int TotalFramesProcessed,
    int QrCodesFound,
    string? ErrorMessage);