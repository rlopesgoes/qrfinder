using Domain.Common;
using Domain.Models;

namespace Application.Ports;

public interface IAnalysisResultWriteOnlyRepository
{
    Task<Result> SaveAsync(AnalysisResult analysis, CancellationToken cancellationToken);
}