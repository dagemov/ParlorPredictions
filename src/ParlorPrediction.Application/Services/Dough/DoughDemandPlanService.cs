using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DoughDemandPlanService : IDoughDemandPlanService
{
    private readonly IDoughDemandPlanRepository _doughDemandPlanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DoughDemandPlanService(
        IDoughDemandPlanRepository doughDemandPlanRepository,
        IUnitOfWork unitOfWork)
    {
        _doughDemandPlanRepository = doughDemandPlanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<DoughDemandPlanListItemResponse>> SearchAsync(
        DayOfWeek? dayOfWeek,
        string? sourceTerm,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var demandPlans = await _doughDemandPlanRepository.SearchAsync(
            dayOfWeek,
            sourceTerm,
            activeOnly,
            cancellationToken);

        return demandPlans
            .Select(MapListItem)
            .ToArray();
    }

    public async Task<DoughDemandPlanDetailResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var demandPlan = await _doughDemandPlanRepository.GetByIdAsync(id, cancellationToken);
        return demandPlan is null ? null : MapDetail(demandPlan);
    }

    public async Task<Guid> CreateAsync(
        SaveDoughDemandPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var demandPlan = new DoughDemandPlan(
            Guid.NewGuid(),
            request.DayOfWeek,
            request.SourceName,
            request.MinDoughBalls,
            request.MaxDoughBalls,
            request.Notes,
            request.IsActive);

        await _doughDemandPlanRepository.AddAsync(demandPlan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return demandPlan.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        SaveDoughDemandPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var demandPlan = await _doughDemandPlanRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("The dough demand plan could not be found.");

        demandPlan.UpdatePlan(
            request.DayOfWeek,
            request.SourceName,
            request.MinDoughBalls,
            request.MaxDoughBalls,
            request.Notes,
            request.IsActive);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var demandPlan = await _doughDemandPlanRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("The dough demand plan could not be found.");

        if (isActive)
        {
            demandPlan.Activate();
        }
        else
        {
            demandPlan.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static DoughDemandPlanListItemResponse MapListItem(DoughDemandPlan demandPlan)
    {
        return new DoughDemandPlanListItemResponse
        {
            Id = demandPlan.Id,
            DayOfWeek = demandPlan.DayOfWeek,
            SourceName = demandPlan.SourceName,
            MinDoughBalls = demandPlan.MinDoughBalls,
            MaxDoughBalls = demandPlan.MaxDoughBalls,
            BaselineDoughBalls = demandPlan.GetBaselineDoughBalls(),
            Notes = demandPlan.Notes,
            IsActive = demandPlan.IsActive,
            UpdatedAtUtc = demandPlan.UpdatedAtUtc
        };
    }

    private static DoughDemandPlanDetailResponse MapDetail(DoughDemandPlan demandPlan)
    {
        return new DoughDemandPlanDetailResponse
        {
            Id = demandPlan.Id,
            DayOfWeek = demandPlan.DayOfWeek,
            SourceName = demandPlan.SourceName,
            MinDoughBalls = demandPlan.MinDoughBalls,
            MaxDoughBalls = demandPlan.MaxDoughBalls,
            Notes = demandPlan.Notes,
            IsActive = demandPlan.IsActive
        };
    }
}
