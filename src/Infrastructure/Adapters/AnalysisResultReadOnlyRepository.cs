using Application.Ports;
using Domain.Common;
using Domain.Models;
using Infrastructure.Adapters.Dtos;
using MongoDB.Driver;

namespace Infrastructure.Adapters;

public class AnalysisResultReadOnlyRepository(IMongoDatabase database) : IAnalysisResultReadOnlyRepository
{
    private readonly IMongoCollection<AnalysisResultDto> _collection = 
        database.GetCollection<AnalysisResultDto>("video_processing");

    public async Task<Result<AnalysisResult>> GetAsync(string videoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _collection
                .Find(x => x.VideoId == videoId)
                .FirstOrDefaultAsync(cancellationToken);

            if (document is null)
                return Result<AnalysisResult>.NoContent();
            
            return document!.ToResult();
        }
        catch (Exception e)
        {
            return e;
        }
    }
}