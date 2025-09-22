using Application.Videos.Ports;
using Domain.Videos;
using MongoDB.Driver;

namespace Infrastructure.Implementations;

public class VideoProcessingRepository : IVideoProcessingRepository
{
    private readonly IMongoCollection<VideoProcessing> _collection;

    public VideoProcessingRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<VideoProcessing>("video_processing");
    }

    public async Task<VideoProcessing?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(x => x.VideoId == videoId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(VideoProcessing videoProcessing, CancellationToken cancellationToken = default)
    {
        var existing = await GetByVideoIdAsync(videoProcessing.VideoId, cancellationToken);
        if (existing == null)
        {
            await _collection.InsertOneAsync(videoProcessing, cancellationToken: cancellationToken);
        }
        else
        {
            await _collection.ReplaceOneAsync(
                x => x.VideoId == videoProcessing.VideoId,
                videoProcessing,
                cancellationToken: cancellationToken);
        }
    }

    public async Task UpdateStatusAsync(string videoId, VideoProcessingStatus status, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        var update = Builders<VideoProcessing>.Update
            .Set(x => x.Status, status);

        if (status == VideoProcessingStatus.Completed)
        {
            update = update.Set(x => x.CompletedAt, DateTime.UtcNow);
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            update = update.Set(x => x.ErrorMessage, errorMessage);
        }

        await _collection.UpdateOneAsync(
            x => x.VideoId == videoId,
            update,
            cancellationToken: cancellationToken);
    }

    public async Task AddQRCodeResultsAsync(string videoId, IEnumerable<QRCodeResult> qrCodes, CancellationToken cancellationToken = default)
    {
        var update = Builders<VideoProcessing>.Update
            .Set(x => x.QRCodes, qrCodes.ToList())
            .Set(x => x.Status, VideoProcessingStatus.Completed)
            .Set(x => x.CompletedAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(
            x => x.VideoId == videoId,
            update,
            cancellationToken: cancellationToken);
    }
}