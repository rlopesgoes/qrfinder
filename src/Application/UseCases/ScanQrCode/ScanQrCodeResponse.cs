namespace Application.Videos.UseCases.ProcessVideo;

public record ScanQrCodeResponse(
    string VideoId,
    DateTimeOffset CompletedAt,
    double ProcessingTimeMs,
    IReadOnlyCollection<QrCodeResponse> QrCodes);
    
public record QrCodeResponse(string Text, string FormattedTimestamp);