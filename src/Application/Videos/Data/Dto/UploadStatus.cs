using Domain;

namespace Application.Videos.Data.Dto;

public sealed record UploadStatus(
    string VideoId, 
    UploadStage Stage,
    long? LastSeq = null, 
    long? ReceivedBytes = null, 
    long? TotalBytes = null,
    DateTime? UpdatedAtUtc = null);