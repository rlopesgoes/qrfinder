using Application.Videos.Data;
using Application.Videos.Data.Dto;
using Domain;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Infrastructure.Implementations;

public sealed class VideoStatusRepository(IMongoDatabase db) : IVideoStatusRepository
{
    private const string VideoStatusCollection = "video_status";
    
    private sealed record UploadStatusDto
    {
        [BsonId] public string VideoId { get; set; } = string.Empty;
        public UploadStage Stage { get; set; }
        public int LastSeq { get; set; }
        public long ReceivedBytes { get; set; }
        public long TotalBytes { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    private IMongoCollection<UploadStatusDto> Collection => db.GetCollection<UploadStatusDto>(VideoStatusCollection);

    public Task UpsertAsync(UploadStatus uploadStatus, CancellationToken cancellationToken)
        => Collection.UpdateOneAsync(x => x.VideoId == uploadStatus.VideoId,
            Builders<UploadStatusDto>.Update
                .SetOnInsert(x => x.VideoId, uploadStatus.VideoId)
                .Set(x => x.Stage, uploadStatus.Stage)
                .Set(x => x.LastSeq, uploadStatus.LastSeq)
                .Set(x => x.ReceivedBytes, uploadStatus.ReceivedBytes)
                .Set(x => x.TotalBytes, uploadStatus.TotalBytes)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
            new UpdateOptions { IsUpsert = true }, cancellationToken);

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