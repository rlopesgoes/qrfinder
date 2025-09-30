namespace Domain.Models;

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