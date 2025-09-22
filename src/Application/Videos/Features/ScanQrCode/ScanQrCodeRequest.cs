using MediatR;

namespace Application.Videos.Features.ScanQrCode;

public record ScanQrCodeRequest(string VideoId, string? MessageType) : IRequest<ProcessVideoResult?>;

public record ProcessVideoResult(
    string VideoId,
    DateTimeOffset CompletedAt,
    double ProcessingTimeMs,
    IReadOnlyCollection<QrCodeResult> QrCodes);

public record QrCodeResult(string Text, string FormattedTimestamp);