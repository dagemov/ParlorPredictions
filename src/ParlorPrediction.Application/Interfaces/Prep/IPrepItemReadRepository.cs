using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Prep;

public interface IPrepItemReadRepository
{
    Task<PrepItem?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
