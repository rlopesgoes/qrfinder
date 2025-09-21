using Domain;

namespace Application.Videos.Data.Dto;

public sealed record UploadStatus(
    string VideoId, 
    UploadStage Stage,
    long LastSeq, 
    long ReceivedBytes, 
    long TotalBytes,
    DateTime UpdatedAtUtc);