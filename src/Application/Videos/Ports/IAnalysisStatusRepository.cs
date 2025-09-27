using Application.Videos.Ports.Dtos;
using Domain.Common;

namespace Application.Videos.Ports;

public interface IAnalysisStatusRepository
{
    Task<Result> UpsertAsync(ProcessStatus processStatus, CancellationToken cancellationToken);
    Task<Result<ProcessStatus>> GetAsync(string videoId, CancellationToken cancellationToken);
}