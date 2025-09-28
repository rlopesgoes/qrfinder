using Domain.Common;
using Domain.Models;

namespace Application.Ports;

public interface IStatusReadOnlyRepository
{
    Task<Result<Status>> GetAsync(string videoId, CancellationToken cancellationToken);
}