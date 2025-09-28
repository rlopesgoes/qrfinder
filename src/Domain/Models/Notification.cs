namespace Domain.Models;

public record Notification(
    string VideoId,
    Stage Stage,
    double ProgressPercentage,
    string? Message = null,
    DateTime Timestamp = default);

