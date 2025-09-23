namespace Domain.Videos.Ports;

public interface IVideoRepository
{
    Task<Video?> GetByIdAsync(VideoId videoId, CancellationToken cancellationToken = default);
    Task SaveAsync(Video video, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Video>> GetByStatusAsync(VideoStatus status, CancellationToken cancellationToken = default);
}