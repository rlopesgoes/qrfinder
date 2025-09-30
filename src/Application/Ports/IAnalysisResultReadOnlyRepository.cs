using Domain.Common;
using Domain.Models;

namespace Application.Ports;

public interface IAnalysisResultReadOnlyRepository
{
    Task<Result<AnalysisResult>> GetAsync(string videoId, CancellationToken cancellationToken);
}