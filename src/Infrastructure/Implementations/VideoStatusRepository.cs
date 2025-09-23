using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using Domain.Videos;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Infrastructure.Implementations;

public sealed class VideoStatusRepository(IMongoDatabase db) : IVideoStatusRepository
{
    private const string VideoStatusCollection = "video_status";
    
    private sealed record UploadStatusDto
    {
        [BsonId] public string VideoId { get; set; } = string.Empty;
        public VideoProcessingStage Stage { get; set; }
        public int LastSeq { get; set; }
        public long ReceivedBytes { get; set; }
        public long TotalBytes { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    private IMongoCollection<UploadStatusDto> Collection => db.GetCollection<UploadStatusDto>(VideoStatusCollection);

    public async Task UpsertAsync(UploadStatus uploadStatus, CancellationToken cancellationToken)
    {
        var builder = Builders<UploadStatusDto>.Update
            .SetOnInsert(x => x.VideoId, uploadStatus.VideoId)
            .Set(x => x.Stage, uploadStatus.Stage)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);
        
        if (uploadStatus.LastSeq is not null)
            builder = builder.Set(x => x.LastSeq, uploadStatus.LastSeq);
        
        if (uploadStatus.ReceivedBytes is not null)
            builder = builder.Set(x => x.ReceivedBytes, uploadStatus.ReceivedBytes);
        
        if (uploadStatus.TotalBytes is not null)
            builder = builder.Set(x => x.TotalBytes, uploadStatus.TotalBytes);
        
        await Collection.UpdateOneAsync(x => x.VideoId == uploadStatus.VideoId,
            builder, new UpdateOptions { IsUpsert = true }, cancellationToken);
    }
    
    public async Task<UploadStatus?> GetAsync(string videoId, CancellationToken cancellationToken)
    {
        var uploadVideoStatus = await Collection.Find(x => x.VideoId == videoId).FirstOrDefaultAsync(cancellationToken);
        return uploadVideoStatus is null ? null :
            new UploadStatus(
                uploadVideoStatus.VideoId, 
                uploadVideoStatus.Stage, 
                uploadVideoStatus.LastSeq, 
                uploadVideoStatus.ReceivedBytes, 
                uploadVideoStatus.TotalBytes, 
                uploadVideoStatus.UpdatedAtUtc);
    }
}