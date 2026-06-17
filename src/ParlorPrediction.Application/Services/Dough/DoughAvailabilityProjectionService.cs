using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DoughAvailabilityProjectionService : IDoughAvailabilityProjectionService
{
    private const int OperationalDays = 6;

    private readonly IDailyDoughClosingRepository _dailyDoughClosingRepository;
    private readonly IDoughSourceProjectionService _doughSourceProjectionService;
    private readonly IDoughInventoryReadRepository _doughInventoryReadRepository;
    private readonly IDoughLossRecordRepository _doughLossRecordRepository;
    private readonly IPrepTaskRepository _prepTaskRepository;
    private readonly IWeeklyDoughClosingReadService _weeklyDoughClosingReadService;

    public DoughAvailabilityProjectionService(
        IDailyDoughClosingRepository dailyDoughClosingRepository,
        IDoughSourceProjectionService doughSourceProjectionService,
        IDoughInventoryReadRepository doughInventoryReadRepository,
        IDoughLossRecordRepository doughLossRecordRepository,
        IPrepTaskRepository prepTaskRepository,
        IWeeklyDoughClosingReadService weeklyDoughClosingReadService)
    {
        _dailyDoughClosingRepository = dailyDoughClosingRepository;
        _doughSourceProjectionService = doughSourceProjectionService;
        _doughInventoryReadRepository = doughInventoryReadRepository;
        _doughLossRecordRepository = doughLossRecordRepository;
        _prepTaskRepository = prepTaskRepository;
        _weeklyDoughClosingReadService = weeklyDoughClosingReadService;
    }

    public async Task<DoughAvailabilityProjectionResponse> GetWeeklyAvailabilityAsync(
        DateOnly referenceDate,
        CancellationToken cancellationToken = default)
    {
        if (referenceDate == default)
        {
            throw new ArgumentException("Reference date is required.", nameof(referenceDate));
        }

        var weekStartDate = GetOperationalWeekStart(referenceDate);
        var weekEndDate = weekStartDate.AddDays(OperationalDays - 1);
        var latestInventorySnapshot = await _doughInventoryReadRepository.GetLatestSnapshotOnOrBeforeAsync(
            referenceDate,
            cancellationToken);
        var carryover = await _weeklyDoughClosingReadService.GetCarryoverForWeekAsync(
            new GetWeeklyDoughCarryoverRequest
            {
                WeekStartDate = referenceDate
            },
            cancellationToken);
        var tasks = await _prepTaskRepository.GetDoughTasksBetweenDatesAsync(
            weekStartDate,
            referenceDate,
            cancellationToken);
        var closings = await _dailyDoughClosingRepository.ListByWeekStartDateAsync(weekStartDate, cancellationToken);
        var losses = await _doughLossRecordRepository.SearchAsync(
            weekStartDate,
            referenceDate,
            null,
            cancellationToken);
        var remainingBySource = await _doughSourceProjectionService.GetRemainingBySourceAsync(
            referenceDate,
            cancellationToken);

        var producedThisWeekBalls = SumProducedBallsWithinWindow(tasks, weekStartDate, referenceDate);
        var actualUsedBallsThisWeek = closings
            .Where(closing => closing.ClosingDate <= referenceDate)
            .Sum(closing => closing.ActualUsedBalls);
        var lostBallsThisWeek = losses.Sum(loss => loss.QuantityLostBalls);
        var availableBalls = ResolveAvailableBalls(
            carryover,
            latestInventorySnapshot,
            weekStartDate,
            producedThisWeekBalls,
            actualUsedBallsThisWeek,
            lostBallsThisWeek);

        var currentMustUseBalls = SumCurrentStatusBalls(remainingBySource, DoughQualityStatus.MustUseNextDay);
        var currentAttentionBalls = SumCurrentStatusBalls(remainingBySource, DoughQualityStatus.Attention);
        var (mustUseRemainingBalls, attentionRemainingBalls, regularReadyBalls) = AllocateAvailableBallsByPriority(
            availableBalls,
            currentMustUseBalls,
            currentAttentionBalls);

        return new DoughAvailabilityProjectionResponse
        {
            ReferenceDate = referenceDate,
            WeekStartDate = weekStartDate,
            WeekEndDate = weekEndDate,
            HasClosingCarryover = carryover.HasClosingCarryover,
            CarryoverSourceWeekStartDate = carryover.SourceWeekStartDate,
            CarryoverSourceWeekEndDate = carryover.SourceWeekEndDate,
            CarryoverReadyBalls = carryover.CarryoverReadyBalls,
            CarryoverAttentionBalls = carryover.CarryoverAttentionBalls,
            CarryoverAvailableBalls = carryover.CarryoverAvailableBalls,
            CarryoverMixedButNotBalledLoads = carryover.MixedButNotBalledLoads,
            PreviousWeekProducedBalls = carryover.PreviousWeekProducedBalls,
            PreviousWeekUsedBalls = carryover.PreviousWeekUsedBalls,
            PreviousWeekLostBalls = carryover.PreviousWeekLostBalls,
            CarryoverClosingNotes = carryover.ClosingNotes,
            ProducedThisWeekBalls = producedThisWeekBalls,
            ActualUsedBallsThisWeek = actualUsedBallsThisWeek,
            LostBallsThisWeek = lostBallsThisWeek,
            AvailableBalls = availableBalls,
            RegularReadyBalls = regularReadyBalls,
            AttentionAvailableBalls = attentionRemainingBalls,
            MustUseNextDayBalls = mustUseRemainingBalls
        };
    }

    private static int ResolveAvailableBalls(
        Contracts.Responses.DoughClosing.WeeklyDoughCarryoverResponse carryover,
        DoughInventorySnapshot? latestInventorySnapshot,
        DateOnly weekStartDate,
        int producedThisWeekBalls,
        int actualUsedBallsThisWeek,
        int lostBallsThisWeek)
    {
        if (carryover.HasClosingCarryover)
        {
            return Math.Max(
                0,
                carryover.CarryoverAvailableBalls + producedThisWeekBalls - actualUsedBallsThisWeek - lostBallsThisWeek);
        }

        var hasCurrentWeekSnapshot = latestInventorySnapshot is not null &&
            latestInventorySnapshot.SnapshotDate >= weekStartDate;

        if (hasCurrentWeekSnapshot)
        {
            return latestInventorySnapshot!.AvailableBalls;
        }

        var fallbackAvailableBalls = latestInventorySnapshot?.AvailableBalls ?? 0;
        return Math.Max(
            0,
            fallbackAvailableBalls + producedThisWeekBalls - actualUsedBallsThisWeek - lostBallsThisWeek);
    }

    private static int SumProducedBallsWithinWindow(
        IReadOnlyList<PrepTask> tasks,
        DateOnly windowStart,
        DateOnly windowEnd)
    {
        return tasks
            .Where(task =>
                task.Status == PrepTaskStatus.Completed &&
                task.TaskType == PrepTaskType.BallDough &&
                task.CompletedAtUtc.HasValue)
            .Where(task =>
            {
                var completedLocalDate = DateOnly.FromDateTime(task.CompletedAtUtc!.Value.ToLocalTime());
                return completedLocalDate >= windowStart && completedLocalDate <= windowEnd;
            })
            .Sum(task => task.CompletedBallsEquivalent);
    }

    private static int SumCurrentStatusBalls(
        IReadOnlyList<Contracts.Responses.DoughUsage.DoughSourceRemainingResponse> qualityRecords,
        DoughQualityStatus status)
    {
        return qualityRecords
            .Where(record => string.Equals(record.SourceType, status.ToString(), StringComparison.OrdinalIgnoreCase))
            .Sum(record => record.RemainingBalls);
    }

    private static (int MustUseRemainingBalls, int AttentionRemainingBalls, int RegularReadyBalls) AllocateAvailableBallsByPriority(
        int availableBalls,
        int grossMustUseBalls,
        int grossAttentionBalls)
    {
        var effectiveMustUseBalls = Math.Min(grossMustUseBalls, availableBalls);
        var availableAfterMustUse = Math.Max(availableBalls - effectiveMustUseBalls, 0);
        var effectiveAttentionBalls = Math.Min(grossAttentionBalls, availableAfterMustUse);
        var regularReadyBalls = Math.Max(availableBalls - effectiveMustUseBalls - effectiveAttentionBalls, 0);

        return (effectiveMustUseBalls, effectiveAttentionBalls, regularReadyBalls);
    }

    private static DateOnly GetOperationalWeekStart(DateOnly referenceDate)
    {
        var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
        return referenceDate.AddDays(-diff);
    }
}
