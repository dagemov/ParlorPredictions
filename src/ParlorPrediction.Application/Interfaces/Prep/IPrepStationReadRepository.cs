using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Prep;

public interface IPrepStationReadRepository
{
    Task<IReadOnlyList<PrepStation>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<PrepStation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
