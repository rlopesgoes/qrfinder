using Domain.Common;

namespace Domain.Videos;

public record VideoProcessingStarted(VideoId VideoId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record VideoProcessingCompleted(
    VideoId VideoId, 
    IReadOnlyList<QrCode> QrCodes,
    ProcessingMetrics Metrics) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record VideoProcessingFailed(
    VideoId VideoId, 
    string ErrorMessage) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record VideoChunkReceived(
    VideoId VideoId,
    int SequenceNumber,
    long TotalBytesReceived) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record VideoUploadCompleted(VideoId VideoId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}