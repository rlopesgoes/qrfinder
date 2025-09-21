using Application.Videos.Data.Dto;

namespace Application.Videos.Data;

public interface IVideoStatusRepository
{
    Task UpsertAsync(UploadStatus uploadStatus, CancellationToken cancellationToken);
    Task<UploadStatus?> GetLastSeqAsync(string videoId, CancellationToken cancellationToken);
}