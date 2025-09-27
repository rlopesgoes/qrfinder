namespace Application.UseCases.ScanQrCode;

public record ScanQrCodeResponse(
    string VideoId,
    DateTimeOffset CompletedAt,
    double ProcessingTimeMs,
    IReadOnlyCollection<QrCodeResponse> QrCodes);
    
public record QrCodeResponse(string Text, string FormattedTimestamp);