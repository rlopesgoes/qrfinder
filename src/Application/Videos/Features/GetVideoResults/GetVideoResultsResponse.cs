using Application.Videos.Ports.Dtos;

namespace Application.Videos.Features.GetVideoResults;

public record GetVideoResultsResponse(
    string VideoId,
    string Status,
    DateTime? CompletedAt,
    int TotalQrCodes,
    IReadOnlyList<QrCodeResultDto> QrCodes);