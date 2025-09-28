using Application.Ports;
using Domain.Common;
using Domain.Models;
using Infrastructure.Adapters.Dtos;
using MongoDB.Driver;

namespace Infrastructure.Adapters;

public sealed class StatusReadOnlyRepository(IMongoDatabase db) : IStatusReadOnlyRepository
{
    private const string VideoStatusCollection = "video_status";

    private IMongoCollection<UploadStatusDto> Collection => 
        db.GetCollection<UploadStatusDto>(VideoStatusCollection);
    
    public async Task<Result<Status>> GetAsync(string videoId, CancellationToken cancellationToken)
    {
        var uploadVideoStatus = await Collection.Find(x => x.VideoId == videoId).FirstOrDefaultAsync(cancellationToken);
        return uploadVideoStatus is null ? Result<Status>.NoContent() : 
            new Status(
                uploadVideoStatus.VideoId, 
                uploadVideoStatus.Stage, 
                uploadVideoStatus.UpdatedAtUtc);
    }
}