using Application.Ports;
using Domain.Common;
using Domain.Models;
using Infrastructure.Adapters.Dtos;
using MongoDB.Driver;

namespace Infrastructure.Adapters;

public class AnalysisResultWriteOnlyRepository(IMongoDatabase database) : IAnalysisResultWriteOnlyRepository
{
    private readonly IMongoCollection<AnalysisResultDto> _collection =
        database.GetCollection<AnalysisResultDto>("video_processing");
    
    public async Task<Result> SaveAsync(AnalysisResult analysis, CancellationToken cancellationToken = default)
    {
        var document = AnalysisResultDto.FromResult(analysis);

        await _collection.ReplaceOneAsync(
            x => x.VideoId == document.VideoId,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
        
        return Result.Success();
    }
}