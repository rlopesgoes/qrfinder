namespace Domain.Videos;

public class VideoProcessing
{
    public string Id { get; set; } = null!;
    public string VideoId { get; set; } = null!;
    public VideoProcessingStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalFramesProcessed { get; set; }
    public List<QRCodeResult> QRCodes { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public enum VideoProcessingStatus
{
    Started,
    Processing,
    Completed,
    Failed
}

public class QRCodeResult
{
    public string Text { get; set; } = null!;
    public double TimestampSeconds { get; set; }
    public string FormattedTime { get; set; } = null!;
    public DateTime DetectedAt { get; set; }
}