namespace Domain.Videos;

public record VideoId(Guid Value)
{
    public static VideoId New() => new(Guid.NewGuid());
    public static VideoId From(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}

public record QrCode(string Content, Timestamp DetectedAt)
{
    public static QrCode Create(string content, double timestampSeconds)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("QR code content cannot be empty");

        if (timestampSeconds < 0)
            throw new ArgumentException("Timestamp cannot be negative");

        return new QrCode(content, new Timestamp(timestampSeconds));
    }

    public string FormattedTimestamp => DetectedAt.ToFormattedString();
}

public record Timestamp(double Seconds)
{
    public static Timestamp Create(double seconds)
    {
        if (seconds < 0)
            throw new ArgumentException("Timestamp cannot be negative");

        return new Timestamp(seconds);
    }

    public string ToFormattedString() => TimeSpan.FromSeconds(Seconds).ToString(@"mm\:ss\.fff");
    public DateTime ToDateTime(DateTime baseTime) => baseTime.AddSeconds(Seconds);
}

public record ProcessingMetrics(
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TotalFramesProcessed,
    TimeSpan ProcessingDuration)
{
    public static ProcessingMetrics Started() => new(
        DateTime.UtcNow, 
        null, 
        0, 
        TimeSpan.Zero);

    public ProcessingMetrics Complete(int framesProcessed) => this with
    {
        CompletedAt = DateTime.UtcNow,
        TotalFramesProcessed = framesProcessed,
        ProcessingDuration = DateTime.UtcNow - StartedAt
    };

    public double ProcessingTimeMs => ProcessingDuration.TotalMilliseconds;
}

public record QrCodeDetection(string Text, double TimestampSeconds);

public record ProcessingResult(
    VideoId VideoId,
    IReadOnlyList<QrCode> QrCodes,
    ProcessingMetrics Metrics);