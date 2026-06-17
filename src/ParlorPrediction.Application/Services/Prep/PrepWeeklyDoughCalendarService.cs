using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Application.Services.Dough;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.Prep;

public sealed class PrepWeeklyDoughCalendarService : IPrepWeeklyDoughCalendarService
{
    private const int OperationalDays = 6;

    private readonly IDoughAvailabilityProjectionService _doughAvailabilityProjectionService;
    private readonly IDoughPrepCalculationService _doughPrepCalculationService;
    private readonly IDoughBatchReadRepository _doughBatchReadRepository;
    private readonly IDoughInventoryReadRepository _doughInventoryReadRepository;
    private readonly IDailyDoughClosingRepository _dailyDoughClosingRepository;
    private readonly IPrepTaskRepository _prepTaskRepository;

    public PrepWeeklyDoughCalendarService(
        IDoughAvailabilityProjectionService doughAvailabilityProjectionService,
        IDoughPrepCalculationService doughPrepCalculationService,
        IDoughBatchReadRepository doughBatchReadRepository,
        IDoughInventoryReadRepository doughInventoryReadRepository,
        IDailyDoughClosingRepository dailyDoughClosingRepository,
        IPrepTaskRepository prepTaskRepository)
    {
        _doughAvailabilityProjectionService = doughAvailabilityProjectionService;
        _doughPrepCalculationService = doughPrepCalculationService;
        _doughBatchReadRepository = doughBatchReadRepository;
        _doughInventoryReadRepository = doughInventoryReadRepository;
        _dailyDoughClosingRepository = dailyDoughClosingRepository;
        _prepTaskRepository = prepTaskRepository;
    }

    public async Task<WeeklyDoughCalendarResponse> GetWeekAsync(
        DateOnly referenceDate,
        int historicalWeeksToUse,
        CancellationToken cancellationToken = default)
    {
        if (referenceDate == default)
        {
            throw new ArgumentException("Reference date is required.", nameof(referenceDate));
        }

        if (historicalWeeksToUse < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(historicalWeeksToUse), "Historical weeks must be at least 1.");
        }

        var weekStartDate = GetOperationalWeekStart(referenceDate);
        var weekEndDate = weekStartDate.AddDays(OperationalDays - 1);
        var previousReferenceStartDate = weekStartDate.AddDays(-7);
        var previousReferenceEndDate = weekStartDate.AddDays(-1);
        var days = new List<WeeklyDoughCalendarDayResponse>(OperationalDays);

        for (var offset = 0; offset < OperationalDays; offset++)
        {
            var day = weekStartDate.AddDays(offset);
            var calculation = await _doughPrepCalculationService.CalculateAsync(
                new CalculateDoughPrepRequest
                {
                    TargetDate = day,
                    HistoricalWeeksToUse = historicalWeeksToUse
                },
                cancellationToken);

            days.Add(new WeeklyDoughCalendarDayResponse
            {
                Date = day,
                RestaurantDoughBalls = calculation.HistoricalAverageBalls,
                EventDoughBalls = calculation.EventEstimatedBalls,
                TotalNeededBalls = calculation.RequiredBalls,
                AvailableBalls = calculation.AvailableBalls,
                CompletedBalls = calculation.CompletedBalls,
                StillMissingBalls = calculation.MissingBalls,
                Status = DetermineStatus(calculation.RequiredBalls, calculation.AvailableBalls, calculation.CompletedBalls, calculation.MissingBalls)
            });
        }

        var weekTotalNeededBalls = days.Sum(day => day.TotalNeededBalls);
        var latestInventorySnapshot = await _doughInventoryReadRepository.GetLatestSnapshotOnOrBeforeAsync(
            referenceDate,
            cancellationToken);
        var doughBatches = await _doughBatchReadRepository.GetProducedOnOrBeforeAsync(
            referenceDate,
            cancellationToken);
        var availability = await _doughAvailabilityProjectionService.GetWeeklyAvailabilityAsync(
            referenceDate,
            cancellationToken);
        var hasCurrentWeekSnapshot = latestInventorySnapshot is not null &&
            latestInventorySnapshot.SnapshotDate >= weekStartDate;
        var applyCarryoverFallback = !hasCurrentWeekSnapshot &&
            availability.HasClosingCarryover;
        var currentWeekTasks = await _prepTaskRepository.GetDoughTasksBetweenDatesAsync(
            weekStartDate,
            weekEndDate,
            cancellationToken);
        var previousReferenceTasks = await _prepTaskRepository.GetDoughTasksBetweenDatesAsync(
            previousReferenceStartDate,
            previousReferenceEndDate,
            cancellationToken);
        var dailyClosings = await _dailyDoughClosingRepository.ListByWeekStartDateAsync(weekStartDate, cancellationToken);
        var closedDailyClosings = dailyClosings
            .Where(closing => closing.ClosingDate <= referenceDate)
            .ToArray();
        var actualUsedBallsThisWeek = closedDailyClosings.Sum(closing => closing.ActualUsedBalls);
        var accumulatedDailyVariance = closedDailyClosings.Sum(closing => closing.DailyVariance);

        var inventoryBreakdown = DoughWeeklyInventoryCalculator.Calculate(
            referenceDate,
            weekEndDate,
            availability.AvailableBalls,
            doughBatches,
            availability.CarryoverMixedButNotBalledLoads,
            applyCarryoverFallback);

        var producedThisWeekBalls = availability.ProducedThisWeekBalls;
        var finishedThisWeekBalls = SumFinishedBallsWithinWindow(
            currentWeekTasks,
            weekStartDate,
            referenceDate);
        var previousWeekFinishedBalls = availability.HasClosingCarryover && availability.PreviousWeekUsedBalls > 0
            ? availability.PreviousWeekUsedBalls
            : SumFinishedBallsWithinWindow(
                previousReferenceTasks,
                previousReferenceStartDate,
                previousReferenceEndDate);
        var stillMissingThisWeekBalls = DoughWeeklyInventoryCalculator.CalculateStillMissingThisWeek(
            weekTotalNeededBalls,
            inventoryBreakdown,
            actualUsedBallsThisWeek);
        var stillFermentingBalls = DoughWeeklyInventoryCalculator.ResolveStillFermentingForDisplay(
            inventoryBreakdown.StillFermentingBalls,
            producedThisWeekBalls);
        var futureBalls = inventoryBreakdown.MixedButNotBalledBalls + stillFermentingBalls;

        return new WeeklyDoughCalendarResponse
        {
            WeekStartDate = weekStartDate,
            WeekEndDate = weekEndDate,
            HasClosingCarryover = availability.HasClosingCarryover,
            CarryoverSourceWeekStartDate = availability.CarryoverSourceWeekStartDate,
            CarryoverSourceWeekEndDate = availability.CarryoverSourceWeekEndDate,
            CarryoverReadyBalls = availability.CarryoverReadyBalls,
            CarryoverAttentionBalls = availability.CarryoverAttentionBalls,
            CarryoverAvailableBalls = availability.CarryoverAvailableBalls,
            CarryoverMixedButNotBalledLoads = availability.CarryoverMixedButNotBalledLoads,
            CarryoverMixedButNotBalledPotentialBalls = availability.CarryoverMixedButNotBalledLoads * DoughRules.StandardBatchBalls,
            PreviousWeekProducedBalls = availability.PreviousWeekProducedBalls,
            PreviousWeekLostBalls = availability.PreviousWeekLostBalls,
            CarryoverClosingNotes = availability.CarryoverClosingNotes,
            WeekTotalNeededBalls = weekTotalNeededBalls,
            ReadyNowBalls = inventoryBreakdown.ReadyNowBalls,
            StillFermentingBalls = stillFermentingBalls,
            MixedButNotBalledBalls = inventoryBreakdown.MixedButNotBalledBalls,
            MixedButNotBalledLoads = inventoryBreakdown.MixedButNotBalledLoads,
            FutureBalls = futureBalls,
            FinishedThisWeekBalls = finishedThisWeekBalls,
            ProducedThisWeekBalls = producedThisWeekBalls,
            PreviousWeekFinishedBalls = previousWeekFinishedBalls,
            StillMissingThisWeekBalls = stillMissingThisWeekBalls,
            ActualUsedBallsThisWeek = actualUsedBallsThisWeek,
            AccumulatedDailyVariance = accumulatedDailyVariance,
            UpcomingEventBalls = days.Sum(day => day.EventDoughBalls),
            Days = days
        };
    }

    private static DateOnly GetOperationalWeekStart(DateOnly referenceDate)
    {
        var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
        return referenceDate.AddDays(-diff);
    }

    private static string DetermineStatus(int totalNeededBalls, int availableBalls, int completedBalls, int stillMissingBalls)
    {
        if (totalNeededBalls <= 0)
        {
            return "No Data";
        }

        if (stillMissingBalls <= 0)
        {
            return "Covered";
        }

        return completedBalls > 0 || availableBalls > 0
            ? "In Progress"
            : "Needs Dough";
    }

    private static int SumFinishedBallsWithinWindow(
        IReadOnlyList<PrepTask> tasks,
        DateOnly windowStart,
        DateOnly windowEnd)
    {
        return tasks
            .Where(task =>
                task.Status == PrepTaskStatus.Completed &&
                task.CountsAsAvailableBallsWhenCompleted &&
                task.CompletedAtUtc.HasValue)
            .Where(task =>
            {
                var completedLocalDate = DateOnly.FromDateTime(task.CompletedAtUtc!.Value.ToLocalTime());
                return completedLocalDate >= windowStart && completedLocalDate <= windowEnd;
            })
            .Sum(task => task.CompletedBallsEquivalent);
    }

}
