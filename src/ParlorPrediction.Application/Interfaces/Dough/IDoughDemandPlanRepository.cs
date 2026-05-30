using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughDemandPlanRepository : IDoughDemandPlanReadRepository
{
    Task AddAsync(DoughDemandPlan doughDemandPlan, CancellationToken cancellationToken = default);

    Task<DoughDemandPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DoughDemandPlan>> SearchAsync(
        DayOfWeek? dayOfWeek,
        string? sourceTerm,
        bool activeOnly,
        CancellationToken cancellationToken = default);
}
