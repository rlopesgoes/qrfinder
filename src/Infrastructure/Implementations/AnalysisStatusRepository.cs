using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using Domain.Common;
using Domain.Videos;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Infrastructure.Implementations;

public sealed class AnalysisStatusRepository(IMongoDatabase db) : IAnalysisStatusRepository
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

    public async Task<Result> UpsertAsync(ProcessStatus processStatus, CancellationToken cancellationToken)
    {
        try
        {
            var builder = Builders<UploadStatusDto>.Update
                .SetOnInsert(x => x.VideoId, processStatus.VideoId)
                .Set(x => x.Stage, processStatus.Stage)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);
        
            var result = await Collection.UpdateOneAsync(x => x.VideoId == processStatus.VideoId,
                builder, new UpdateOptions { IsUpsert = true }, cancellationToken);
            
            if (!result.IsAcknowledged)
                return Result.WithError($"Error on upsert into db. VideoId: {processStatus.VideoId}");
            
            return Result.Success();
        }
        catch (Exception e)
        {
            return e;
        }
    }
    
    public async Task<Result<ProcessStatus>> GetAsync(string videoId, CancellationToken cancellationToken)
    {
        var uploadVideoStatus = await Collection.Find(x => x.VideoId == videoId).FirstOrDefaultAsync(cancellationToken);
        return uploadVideoStatus is null ? Result<ProcessStatus>.NoContent() : 
            new ProcessStatus(
                uploadVideoStatus.VideoId, 
                uploadVideoStatus.Stage, 
                uploadVideoStatus.LastSeq, 
                uploadVideoStatus.ReceivedBytes, 
                uploadVideoStatus.TotalBytes, 
                uploadVideoStatus.UpdatedAtUtc);
    }
}