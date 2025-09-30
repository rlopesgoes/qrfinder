namespace Domain.Models;

public sealed record Status(
    string VideoId, 
    Stage Stage,
    DateTime? UpdatedAtUtc = null);