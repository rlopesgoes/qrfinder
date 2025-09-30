using Domain.Common;
using Domain.Models;

namespace Application.Ports;

public interface IStatusWriteOnlyRepository
{
    Task<Result> UpsertAsync(Status status, CancellationToken cancellationToken);
}