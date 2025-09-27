using Domain.Videos;

namespace Application.Videos.Ports.Dtos;

public sealed record ProcessStatus(
    string VideoId, 
    VideoProcessingStage Stage,
    long? LastSeq = null, 
    long? ReceivedBytes = null, 
    long? TotalBytes = null,
    DateTime? UpdatedAtUtc = null,
    string? UploadUrl = null,
    DateTime ExpiresAt = default);