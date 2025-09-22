using Domain.Videos;

namespace Application.Videos.Ports.Dtos;

public sealed record UploadStatus(
    string VideoId, 
    UploadStage Stage,
    long? LastSeq = null, 
    long? ReceivedBytes = null, 
    long? TotalBytes = null,
    DateTime? UpdatedAtUtc = null);