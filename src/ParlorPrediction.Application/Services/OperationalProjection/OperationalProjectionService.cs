using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Services.OperationalProjection;

public sealed class OperationalProjectionService : IOperationalProjectionService
{
    private const string DailyClosingSourceType = "DailyClosing";
    private const string RestaurantEventSourceType = "RestaurantEvent";

    private readonly IConsumptionLedgerRepository _consumptionLedgerRepository;
    private readonly IDailyDoughClosingReadService _dailyDoughClosingReadService;
    private readonly IDoughAvailabilityProjectionService _doughAvailabilityProjectionService;
    private readonly IDoughInventoryImpactReadService _doughInventoryImpactReadService;
    private readonly IInventoryTransformationLedgerRepository _inventoryTransformationLedgerRepository;
    private readonly IPrepWeeklyDoughCalendarService _prepWeeklyDoughCalendarService;
    private readonly IProductionLedgerRepository _productionLedgerRepository;

    public OperationalProjectionService(
        IConsumptionLedgerRepository consumptionLedgerRepository,
        IDailyDoughClosingReadService dailyDoughClosingReadService,
        IDoughAvailabilityProjectionService doughAvailabilityProjectionService,
        IDoughInventoryImpactReadService doughInventoryImpactReadService,
        IInventoryTransformationLedgerRepository inventoryTransformationLedgerRepository,
        IPrepWeeklyDoughCalendarService prepWeeklyDoughCalendarService,
        IProductionLedgerRepository productionLedgerRepository)
    {
        _consumptionLedgerRepository = consumptionLedgerRepository;
        _dailyDoughClosingReadService = dailyDoughClosingReadService;
        _doughAvailabilityProjectionService = doughAvailabilityProjectionService;
        _doughInventoryImpactReadService = doughInventoryImpactReadService;
        _inventoryTransformationLedgerRepository = inventoryTransformationLedgerRepository;
        _prepWeeklyDoughCalendarService = prepWeeklyDoughCalendarService;
        _productionLedgerRepository = productionLedgerRepository;
    }

    public async Task<OperationalProjectionResult> ProjectAsync(
        OperationalProjectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ReferenceDate == default)
        {
            throw new ArgumentException("Reference date is required.", nameof(request));
        }

        var correlationId = request.CorrelationId ?? Guid.NewGuid();
        var historicalWeeksToUse = request.HistoricalWeeksToUse < 1
            ? 8
            : request.HistoricalWeeksToUse;
        var weeklyGoal = await _prepWeeklyDoughCalendarService.GetWeekAsync(
            request.ReferenceDate,
            historicalWeeksToUse,
            cancellationToken);
        var availability = await _doughAvailabilityProjectionService.GetWeeklyAvailabilityAsync(
            request.ReferenceDate,
            cancellationToken);
        var inventoryImpact = await _doughInventoryImpactReadService.GetInventoryImpactAsync(
            new GetDoughInventoryImpactRequest
            {
                ReferenceDate = request.ReferenceDate,
                HistoricalWeeksToUse = historicalWeeksToUse
            },
            cancellationToken);
        var dailySummary = await _dailyDoughClosingReadService.GetWeekSummaryAsync(
            new GetDailyClosingWeekSummaryRequest
            {
                ReferenceDate = request.ReferenceDate,
                HistoricalWeeksToUse = historicalWeeksToUse
            },
            cancellationToken);
        var productionEntries = await _productionLedgerRepository.ListByOccurredOnRangeAsync(
            weeklyGoal.WeekStartDate,
            weeklyGoal.WeekEndDate,
            cancellationToken);
        var consumptionEntries = await _consumptionLedgerRepository.ListByOccurredOnRangeAsync(
            weeklyGoal.WeekStartDate,
            weeklyGoal.WeekEndDate,
            cancellationToken);
        var transformationEntries = await _inventoryTransformationLedgerRepository.ListByOccurredOnRangeAsync(
            weeklyGoal.WeekStartDate,
            weeklyGoal.WeekEndDate,
            cancellationToken);

        var productionLedger = BuildProductionLedgerSummary(productionEntries);
        var consumptionLedger = BuildConsumptionLedgerSummary(consumptionEntries);
        var transformationLedger = BuildInventoryTransformationSummary(transformationEntries);
        var readyNowBalls = weeklyGoal.ReadyNowBalls;
        var ballsReadyForService = availability.AvailableBalls;
        var remainingWeekDemandBalls = Math.Max(weeklyGoal.WeekTotalNeededBalls - weeklyGoal.ActualUsedBallsThisWeek, 0);
        var projectedCoverageBalls = checked(ballsReadyForService + weeklyGoal.FutureBalls);
        var projectedShortageBalls = Math.Max(remainingWeekDemandBalls - projectedCoverageBalls, 0);
        var projectedSurplusBalls = Math.Max(projectedCoverageBalls - remainingWeekDemandBalls, 0);
        var weeklyClosingUsageConsistent =
            consumptionLedger.ServiceUsageBalls == 0 ||
            consumptionLedger.ServiceUsageBalls == weeklyGoal.ActualUsedBallsThisWeek;
        var warnings = BuildWarnings(
            weeklyGoal,
            availability.AvailableBalls,
            inventoryImpact.ReadyNowBalls,
            consumptionLedger,
            transformationLedger,
            weeklyClosingUsageConsistent);

        return new OperationalProjectionResult
        {
            CorrelationId = correlationId,
            ReferenceDate = request.ReferenceDate,
            WeekStartDate = weeklyGoal.WeekStartDate,
            WeekEndDate = weeklyGoal.WeekEndDate,
            ReadyNowBalls = readyNowBalls,
            BallsReadyForService = ballsReadyForService,
            FutureBalls = weeklyGoal.FutureBalls,
            MixedButNotBalledBalls = weeklyGoal.MixedButNotBalledBalls,
            StillFermentingBalls = weeklyGoal.StillFermentingBalls,
            RemainingWeekDemandBalls = remainingWeekDemandBalls,
            ProjectedCoverageBalls = projectedCoverageBalls,
            ProjectedShortageBalls = projectedShortageBalls,
            ProjectedSurplusBalls = projectedSurplusBalls,
            WeeklyClosingUsageConsistent = weeklyClosingUsageConsistent,
            ProductionLedger = productionLedger,
            ConsumptionLedger = consumptionLedger,
            InventoryTransformationLedger = transformationLedger,
            Days = dailySummary.Days
                .Select(day => new OperationalProjectionDayView
                {
                    Date = day.Date,
                    ForecastNeededBalls = day.ForecastNeededBalls,
                    ActualUsedBalls = day.ActualUsedBalls,
                    RemainingDemandBalls = day.IsClosed
                        ? 0
                        : day.ForecastNeededBalls,
                    IsClosed = day.IsClosed,
                    IsToday = day.IsToday,
                    IsFuture = day.IsFuture,
                    Notes = day.Notes
                })
                .ToArray(),
            ValidationWarnings = warnings
        };
    }

    private static ProductionLedgerSummary BuildProductionLedgerSummary(IReadOnlyList<ProductionLedger> entries)
    {
        var latestEntries = entries
            .GroupBy(entry => new { entry.SourceType, entry.SourceEntityId })
            .Select(group => group
                .OrderByDescending(entry => entry.CreatedAtUtc)
                .First())
            .ToArray();

        return new ProductionLedgerSummary
        {
            EntryCount = latestEntries.Length,
            TotalBallsCreated = latestEntries.Sum(entry => entry.TotalBallsCreated),
            BallsCompleted = latestEntries.Sum(entry => entry.BallsCompleted),
            BallsReballed = latestEntries.Sum(entry => entry.BallsReballed),
            BallsDiscarded = latestEntries.Sum(entry => entry.BallsDiscarded)
        };
    }

    private static ConsumptionLedgerSummary BuildConsumptionLedgerSummary(IReadOnlyList<ConsumptionLedger> entries)
    {
        var latestEntries = entries
            .GroupBy(entry => new { entry.SourceType, entry.SourceEntityId })
            .Select(group => group
                .OrderByDescending(entry => entry.CreatedAtUtc)
                .First())
            .ToArray();
        var activeDailyClosings = latestEntries
            .Where(entry => entry.IsActive && string.Equals(entry.SourceType, DailyClosingSourceType, StringComparison.Ordinal))
            .ToArray();
        var activeEvents = latestEntries
            .Where(entry => entry.IsActive && string.Equals(entry.SourceType, RestaurantEventSourceType, StringComparison.Ordinal))
            .ToArray();
        var potentialEventDoubleCountBalls = activeEvents
            .Where(eventEntry => activeDailyClosings.Any(closing => closing.OccurredOn == eventEntry.OccurredOn))
            .Sum(eventEntry => eventEntry.EventBalls);

        return new ConsumptionLedgerSummary
        {
            EntryCount = latestEntries.Length,
            SalesBalls = activeDailyClosings.Sum(entry => entry.SalesBalls),
            EventBalls = activeEvents.Sum(entry => entry.EventBalls),
            ServiceUsageBalls = activeDailyClosings.Sum(entry => entry.ServiceUsageBalls),
            PotentialEventDoubleCountBalls = potentialEventDoubleCountBalls
        };
    }

    private static InventoryTransformationLedgerSummary BuildInventoryTransformationSummary(
        IReadOnlyList<InventoryTransformationLedger> entries)
    {
        var latestEntries = entries
            .GroupBy(entry => new { entry.SourceType, entry.SourceEntityId })
            .Select(group => group
                .OrderByDescending(entry => entry.CreatedAtUtc)
                .First())
            .ToArray();

        return new InventoryTransformationLedgerSummary
        {
            EntryCount = latestEntries.Length,
            BallsRecovered = latestEntries.Sum(entry => entry.BallsRecovered),
            BallsDiscarded = latestEntries.Sum(entry => entry.BallsDiscarded),
            BallsReclassified = latestEntries.Sum(entry => entry.BallsReclassified)
        };
    }

    private static IReadOnlyList<OperationalValidationWarning> BuildWarnings(
        Contracts.Responses.Prep.WeeklyDoughCalendarResponse weeklyGoal,
        int availabilityReadyBalls,
        int inventoryImpactReadyBalls,
        ConsumptionLedgerSummary consumptionLedger,
        InventoryTransformationLedgerSummary transformationLedger,
        bool weeklyClosingUsageConsistent)
    {
        var warnings = new List<OperationalValidationWarning>();

        if (weeklyGoal.ReadyNowBalls != availabilityReadyBalls || weeklyGoal.ReadyNowBalls != inventoryImpactReadyBalls)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "projection-ready-now-read-model-mismatch",
                Message = "Projection sources disagree on ReadyNow, so planning output should be reviewed before drafting an adjustment.",
                RequiresHumanReview = true
            });
        }

        if (!weeklyClosingUsageConsistent)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "projection-weekly-closing-consistency",
                Message = $"Consumption ledger service usage totals {consumptionLedger.ServiceUsageBalls} balls, but Weekly Closing operational truth shows {weeklyGoal.ActualUsedBallsThisWeek}.",
                RequiresHumanReview = true
            });
        }

        if (consumptionLedger.PotentialEventDoubleCountBalls > 0)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "projection-event-double-count-risk",
                Message = $"External event demand totals {consumptionLedger.PotentialEventDoubleCountBalls} balls on dates that also have daily service usage closings. Projection keeps ReadyNow unchanged and does not add those event balls twice.",
                RequiresHumanReview = true
            });
        }

        if (transformationLedger.BallsRecovered > 0 || transformationLedger.BallsDiscarded > 0 || transformationLedger.BallsReclassified > 0)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "projection-transformations-informational",
                Message = "Inventory transformations are informational in this planning view. They do not mutate ReadyNow inside the projection layer.",
                RequiresHumanReview = false
            });
        }

        return warnings;
    }
}
