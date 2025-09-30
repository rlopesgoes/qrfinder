namespace Domain.Models;

public record ProgressNotification(
    string VideoId, 
    string Stage, 
    double ProgressPercentage = 0, 
    string? Message = null);