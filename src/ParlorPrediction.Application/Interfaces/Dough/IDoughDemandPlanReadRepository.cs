using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughDemandPlanReadRepository
{
    Task<IReadOnlyCollection<DoughDemandPlan>> GetActiveByDayOfWeekAsync(
        DayOfWeek dayOfWeek,
        CancellationToken cancellationToken = default);
}
