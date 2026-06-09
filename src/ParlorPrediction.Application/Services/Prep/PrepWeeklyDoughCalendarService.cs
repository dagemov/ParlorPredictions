using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
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

    private readonly IDoughPrepCalculationService _doughPrepCalculationService;
    private readonly IDoughBatchReadRepository _doughBatchReadRepository;
    private readonly IDoughInventoryReadRepository _doughInventoryReadRepository;
    private readonly IPrepTaskRepository _prepTaskRepository;
    private readonly IWeeklyDoughClosingReadService _weeklyDoughClosingReadService;

    public PrepWeeklyDoughCalendarService(
        IDoughPrepCalculationService doughPrepCalculationService,
        IDoughBatchReadRepository doughBatchReadRepository,
        IDoughInventoryReadRepository doughInventoryReadRepository,
        IPrepTaskRepository prepTaskRepository,
        IWeeklyDoughClosingReadService weeklyDoughClosingReadService)
    {
        _doughPrepCalculationService = doughPrepCalculationService;
        _doughBatchReadRepository = doughBatchReadRepository;
        _doughInventoryReadRepository = doughInventoryReadRepository;
        _prepTaskRepository = prepTaskRepository;
        _weeklyDoughClosingReadService = weeklyDoughClosingReadService;
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
        var carryover = await GetCarryoverPreviewAsync(referenceDate, cancellationToken);
        var hasCurrentWeekSnapshot = latestInventorySnapshot is not null &&
            latestInventorySnapshot.SnapshotDate >= weekStartDate;
        var applyCarryoverFallback = !hasCurrentWeekSnapshot && carryover?.HasClosingCarryover == true;
        var currentWeekTasks = await _prepTaskRepository.GetDoughTasksBetweenDatesAsync(
            weekStartDate,
            weekEndDate,
            cancellationToken);
        var previousReferenceTasks = await _prepTaskRepository.GetDoughTasksBetweenDatesAsync(
            previousReferenceStartDate,
            previousReferenceEndDate,
            cancellationToken);

        var readyNowBalls = applyCarryoverFallback
            ? carryover!.CarryoverAvailableBalls
            : latestInventorySnapshot?.AvailableBalls ?? 0;
        var stillFermentingBalls = doughBatches
            .Where(batch =>
                batch.FermentationReadyDate > referenceDate &&
                batch.FermentationReadyDate <= weekEndDate)
            .Sum(batch => batch.TotalBalls);
        var liveMixedButNotBalledBalls = doughBatches
            .Where(batch =>
                !batch.IsBalled &&
                batch.FermentationReadyDate <= referenceDate)
            .Sum(batch => batch.TotalBalls);
        var carryoverMixedButNotBalledBalls = applyCarryoverFallback && liveMixedButNotBalledBalls == 0
            ? carryover?.MixedButNotBalledLoads * DoughRules.StandardBatchBalls ?? 0
            : 0;
        var mixedButNotBalledBalls = liveMixedButNotBalledBalls + carryoverMixedButNotBalledBalls;
        var finishedThisWeekBalls = SumFinishedBallsWithinWindow(
            currentWeekTasks,
            weekStartDate,
            referenceDate);
        var previousWeekFinishedBalls = carryover?.HasClosingCarryover == true && carryover.PreviousWeekUsedBalls > 0
            ? carryover.PreviousWeekUsedBalls
            : SumFinishedBallsWithinWindow(
                previousReferenceTasks,
                previousReferenceStartDate,
                previousReferenceEndDate);
        var stillMissingThisWeekBalls = Math.Max(
            weekTotalNeededBalls - readyNowBalls - stillFermentingBalls - mixedButNotBalledBalls,
            0);

        return new WeeklyDoughCalendarResponse
        {
            WeekStartDate = weekStartDate,
            WeekEndDate = weekEndDate,
            HasClosingCarryover = carryover?.HasClosingCarryover ?? false,
            CarryoverSourceWeekStartDate = carryover?.SourceWeekStartDate,
            CarryoverSourceWeekEndDate = carryover?.SourceWeekEndDate,
            CarryoverReadyBalls = carryover?.CarryoverReadyBalls ?? 0,
            CarryoverAttentionBalls = carryover?.CarryoverAttentionBalls ?? 0,
            CarryoverAvailableBalls = carryover?.CarryoverAvailableBalls ?? 0,
            CarryoverMixedButNotBalledLoads = carryover?.MixedButNotBalledLoads ?? 0,
            CarryoverMixedButNotBalledPotentialBalls = (carryover?.MixedButNotBalledLoads ?? 0) * DoughRules.StandardBatchBalls,
            PreviousWeekProducedBalls = carryover?.PreviousWeekProducedBalls ?? 0,
            PreviousWeekLostBalls = carryover?.PreviousWeekLostBalls ?? 0,
            CarryoverClosingNotes = carryover?.ClosingNotes,
            WeekTotalNeededBalls = weekTotalNeededBalls,
            ReadyNowBalls = readyNowBalls,
            StillFermentingBalls = stillFermentingBalls,
            MixedButNotBalledBalls = mixedButNotBalledBalls,
            FinishedThisWeekBalls = finishedThisWeekBalls,
            PreviousWeekFinishedBalls = previousWeekFinishedBalls,
            StillMissingThisWeekBalls = stillMissingThisWeekBalls,
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

    private async Task<Contracts.Responses.DoughClosing.WeeklyDoughCarryoverResponse?> GetCarryoverPreviewAsync(
        DateOnly referenceDate,
        CancellationToken cancellationToken)
    {
        var carryover = await _weeklyDoughClosingReadService.GetCarryoverForWeekAsync(
            new Contracts.Requests.DoughClosing.GetWeeklyDoughCarryoverRequest
            {
                WeekStartDate = referenceDate
            },
            cancellationToken);

        return carryover.HasClosingCarryover
            ? carryover
            : null;
    }
}
