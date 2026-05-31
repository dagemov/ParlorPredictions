using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Prep;

public interface IPrepItemReadRepository
{
    Task<IReadOnlyList<PrepItem>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<PrepItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PrepItem?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
