using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughUsageTraceRepository
{
    Task AddAsync(DoughUsageTrace trace, CancellationToken cancellationToken = default);

    Task<DoughUsageTrace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoughUsageTrace>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoughUsageTrace>> SearchAsync(
        DateOnly? usageDateFrom,
        DateOnly? usageDateTo,
        Guid? sourceDoughBatchQualityRecordId,
        CancellationToken cancellationToken = default);

    void Update(DoughUsageTrace trace);

    void Remove(DoughUsageTrace trace);
}
