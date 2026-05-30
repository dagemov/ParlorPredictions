using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughDemandPlanService
{
    Task<IReadOnlyList<DoughDemandPlanListItemResponse>> SearchAsync(
        DayOfWeek? dayOfWeek,
        string? sourceTerm,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<DoughDemandPlanDetailResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        SaveDoughDemandPlanRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid id,
        SaveDoughDemandPlanRequest request,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default);
}
