using Domain.Models;

namespace Infrastructure.Adapters.Dtos;

internal class AnalysisResultDto
{
    public string Id { get; set; } = null!;
    public string VideoId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double ProcessingTimeMs { get; set; }
    public List<QrCodeDto> QrCodes { get; set; } = new();

    public static AnalysisResultDto FromResult(AnalysisResult result)
    {
        return new AnalysisResultDto
        {
            Id = result.VideoId,
            VideoId = result.VideoId,
            Status = result.Status,
            StartedAt = result.StartedAt,
            CompletedAt = result.CompletedAt,
            ProcessingTimeMs = result.ProcessingTimeMs,
            QrCodes = result.QrCodes.Values.Select(qr => new QrCodeDto
            {
                Text = qr.Content,
                TimestampSeconds = qr.TimeStamp.Seconds,
                FormattedTimestamp = qr.FormattedTimestamp,
                DetectedAt = DateTime.UtcNow
            }).ToList()
        };
    }

    public AnalysisResult ToResult()
    {
        return new AnalysisResult(
            VideoId,
            Status,
            StartedAt,
            CompletedAt,
            ProcessingTimeMs,
            QrCodes.Count,
            new QrCodes(QrCodes.Select(qr => new QrCode(
                qr.Text,
                new (qr.TimestampSeconds))).ToList()));
    }
}

internal class QrCodeDto
{
    public string Text { get; set; } = null!;
    public double TimestampSeconds { get; set; }
    public string FormattedTimestamp { get; set; } = null!;
    public DateTime DetectedAt { get; set; }
}