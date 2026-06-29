using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Dough;

namespace ParlorPrediction.Application.Services.OperationalSimulation;

public sealed class OperationalWeeklyGoalExplanationService : IOperationalWeeklyGoalExplanationService
{
    private readonly IDoughAvailabilityProjectionService _doughAvailabilityProjectionService;
    private readonly IDoughInventoryImpactReadService _doughInventoryImpactReadService;
    private readonly IPrepWeeklyDoughCalendarService _prepWeeklyDoughCalendarService;

    public OperationalWeeklyGoalExplanationService(
        IDoughAvailabilityProjectionService doughAvailabilityProjectionService,
        IDoughInventoryImpactReadService doughInventoryImpactReadService,
        IPrepWeeklyDoughCalendarService prepWeeklyDoughCalendarService)
    {
        _doughAvailabilityProjectionService = doughAvailabilityProjectionService;
        _doughInventoryImpactReadService = doughInventoryImpactReadService;
        _prepWeeklyDoughCalendarService = prepWeeklyDoughCalendarService;
    }

    public async Task<WeeklyGoalExplanationResult> ExplainAsync(
        DateOnly referenceDate,
        int historicalWeeksToUse,
        CancellationToken cancellationToken = default)
    {
        if (referenceDate == default)
        {
            throw new ArgumentException("Reference date is required.", nameof(referenceDate));
        }

        var effectiveHistoricalWeeksToUse = historicalWeeksToUse < 1
            ? 8
            : historicalWeeksToUse;
        var weeklyGoal = await _prepWeeklyDoughCalendarService.GetWeekAsync(
            referenceDate,
            effectiveHistoricalWeeksToUse,
            cancellationToken);
        var availability = await _doughAvailabilityProjectionService.GetWeeklyAvailabilityAsync(
            referenceDate,
            cancellationToken);
        var inventoryImpact = await _doughInventoryImpactReadService.GetInventoryImpactAsync(
            new GetDoughInventoryImpactRequest
            {
                ReferenceDate = referenceDate,
                HistoricalWeeksToUse = effectiveHistoricalWeeksToUse
            },
            cancellationToken);

        var explanation =
            $"Week goal {weeklyGoal.WeekStartDate:yyyy-MM-dd} to {weeklyGoal.WeekEndDate:yyyy-MM-dd}: " +
            $"{weeklyGoal.WeekTotalNeededBalls} needed, {weeklyGoal.ReadyNowBalls} ready now, " +
            $"{weeklyGoal.FutureBalls} future balls, {weeklyGoal.MixedButNotBalledBalls} mixed-but-not-balled, " +
            $"{weeklyGoal.StillMissingThisWeekBalls} still missing. " +
            $"Inventory impact currently shows {inventoryImpact.ReadyNowBalls} ready now and {inventoryImpact.UsedTodayBalls} used today.";

        return new WeeklyGoalExplanationResult
        {
            Explanation = explanation,
            WeeklyGoal = weeklyGoal,
            Availability = availability,
            InventoryImpact = inventoryImpact
        };
    }
}
