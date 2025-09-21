using Application.Videos.Data.Dto;
using Domain.Videos;

namespace Application.Videos.Data;

public interface IVideoStatusRepository
{
    Task UpsertAsync(UploadStatus uploadStatus, CancellationToken cancellationToken);
    Task<UploadStatus?> GetAsync(string videoId, CancellationToken cancellationToken);
}