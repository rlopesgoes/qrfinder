using Application.Ports;
using Domain.Common;
using Domain.Models;
using Infrastructure.Adapters.Dtos;
using MongoDB.Driver;

namespace Infrastructure.Adapters;

public sealed class StatusWriteOnlyRepository(IMongoDatabase db) : IStatusWriteOnlyRepository
{
    private const string VideoStatusCollection = "video_status";
    private IMongoCollection<UploadStatusDto> Collection =>
        db.GetCollection<UploadStatusDto>(VideoStatusCollection);

    public async Task<Result> UpsertAsync(Status status, CancellationToken cancellationToken)
    {
        try
        {
            var builder = Builders<UploadStatusDto>.Update
                .SetOnInsert(x => x.VideoId, status.VideoId)
                .Set(x => x.Stage, status.Stage)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);
        
            var result = await Collection.UpdateOneAsync(x => x.VideoId == status.VideoId,
                builder, new UpdateOptions { IsUpsert = true }, cancellationToken);
            
            if (!result.IsAcknowledged)
                return Result.WithError($"Error on upsert into db. VideoId: {status.VideoId}");
            
            return Result.Success();
        }
        catch (Exception e)
        {
            return e;
        }
    }
}