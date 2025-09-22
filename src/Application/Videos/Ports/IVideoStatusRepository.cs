using Application.Videos.Ports.Dtos;

namespace Application.Videos.Ports;

public interface IVideoStatusRepository
{
    Task UpsertAsync(UploadStatus uploadStatus, CancellationToken cancellationToken);
    Task<UploadStatus?> GetAsync(string videoId, CancellationToken cancellationToken);
}