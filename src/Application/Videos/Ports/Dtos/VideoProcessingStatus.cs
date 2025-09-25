using Domain.Videos;

namespace Application.Videos.Ports.Dtos;

/// <summary>
/// Complete video processing status (upload + processing)
/// Maps to UploadStatus in MongoDB but abstracts technical details
/// </summary>
public sealed record VideoProcessingStatus(
    string VideoId,
    VideoProcessingStage Stage,
    double ProgressPercentage = 0.0,
    string? CurrentOperation = null,
    string? ErrorMessage = null,
    DateTime? UpdatedAtUtc = null);