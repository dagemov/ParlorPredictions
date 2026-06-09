using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughLossRecordRepository
{
    Task AddAsync(DoughLossRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoughLossRecord>> SearchAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        DoughLossReason? lossReason,
        CancellationToken cancellationToken = default);
}
