using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughUsage;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DoughSourceProjectionService : IDoughSourceProjectionService
{
    private readonly IDoughBatchQualityRepository _doughBatchQualityRepository;
    private readonly IDailyDoughClosingRepository _dailyDoughClosingRepository;
    private readonly IDoughUsageTraceRepository _doughUsageTraceRepository;
    private readonly IWeeklyDoughClosingReadService _weeklyDoughClosingReadService;

    public DoughSourceProjectionService(
        IDoughBatchQualityRepository doughBatchQualityRepository,
        IDailyDoughClosingRepository dailyDoughClosingRepository,
        IDoughUsageTraceRepository doughUsageTraceRepository,
        IWeeklyDoughClosingReadService weeklyDoughClosingReadService)
    {
        _doughBatchQualityRepository = doughBatchQualityRepository;
        _dailyDoughClosingRepository = dailyDoughClosingRepository;
        _doughUsageTraceRepository = doughUsageTraceRepository;
        _weeklyDoughClosingReadService = weeklyDoughClosingReadService;
    }

    public async Task<IReadOnlyList<DoughSourceRemainingResponse>> GetRemainingBySourceAsync(
        DateOnly referenceDate,
        CancellationToken cancellationToken = default)
    {
        if (referenceDate == default)
        {
            throw new ArgumentException("Reference date is required.", nameof(referenceDate));
        }

        var records = await ListRelevantRecordsAsync(referenceDate, cancellationToken);
        if (records.Count == 0)
        {
            return Array.Empty<DoughSourceRemainingResponse>();
        }

        var weekStartDate = GetOperationalWeekStart(referenceDate);
        var carryover = await _weeklyDoughClosingReadService.GetCarryoverForWeekAsync(
            new GetWeeklyDoughCarryoverRequest
            {
                WeekStartDate = referenceDate
            },
            cancellationToken);
        var closings = await _dailyDoughClosingRepository.SearchAsync(
            weekStartDate,
            referenceDate,
            cancellationToken);
        var traces = await _doughUsageTraceRepository.SearchAsync(
            weekStartDate,
            referenceDate,
            null,
            cancellationToken);

        var states = records
            .Select(record => new SourceProjectionState(record, weekStartDate))
            .OrderBy(state => state.Record.SourceDate)
            .ThenBy(state => state.Record.CreatedOrBalledAt)
            .ToArray();

        ApplyCarryoverCap(states, weekStartDate, carryover);
        ApplyCurrentWeekUsage(states, weekStartDate, referenceDate, closings, traces);

        return states
            .Select(state => MapLive(state, referenceDate))
            .OrderBy(item => item.SourceDate)
            .ThenBy(item => item.CreatedOrBalledAt)
            .ToArray();
    }

    public async Task<IReadOnlyList<DoughSourceRemainingResponse>> GetTraceableRemainingBySourceAsync(
        DateOnly referenceDate,
        CancellationToken cancellationToken = default)
    {
        if (referenceDate == default)
        {
            throw new ArgumentException("Reference date is required.", nameof(referenceDate));
        }

        var records = await ListRelevantRecordsAsync(referenceDate, cancellationToken);
        if (records.Count == 0)
        {
            return Array.Empty<DoughSourceRemainingResponse>();
        }

        var traces = await _doughUsageTraceRepository.SearchAsync(
            null,
            referenceDate,
            null,
            cancellationToken);

        var usedBySource = traces
            .Where(trace => trace.UsageDate <= referenceDate)
            .GroupBy(trace => trace.SourceDoughBatchQualityRecordId)
            .ToDictionary(group => group.Key, group => group.Sum(trace => trace.BallsUsed));

        return records
            .Select(record => Map(record, referenceDate, usedBySource.GetValueOrDefault(record.Id)))
            .OrderBy(item => item.SourceDate)
            .ThenBy(item => item.CreatedOrBalledAt)
            .ToArray();
    }

    private async Task<IReadOnlyList<DoughBatchQualityRecord>> ListRelevantRecordsAsync(
        DateOnly referenceDate,
        CancellationToken cancellationToken)
    {
        var records = await _doughBatchQualityRepository.ListAsync(cancellationToken);
        return records
            .Where(record => DateOnly.FromDateTime(record.CreatedOrBalledAt.ToLocalTime()) <= referenceDate)
            .OrderBy(record => record.SourceDate)
            .ThenBy(record => record.CreatedOrBalledAt)
            .ToArray();
    }

    private static DoughSourceRemainingResponse Map(
        DoughBatchQualityRecord record,
        DateOnly referenceDate,
        int usedBalls)
    {
        var ageDays = DoughQualityRules.CalculateOperationalAgeDays(record.CreatedOrBalledAt.ToLocalTime(), referenceDate);
        var remainingBalls = record.CountsAsAvailable
            ? Math.Max(record.QuantityBalls - usedBalls, 0)
            : 0;
        var recommendation = DetermineRecommendation(record, referenceDate, ageDays, remainingBalls);

        return new DoughSourceRemainingResponse
        {
            SourceDoughBatchQualityRecordId = record.Id,
            SourceDate = record.SourceDate,
            CreatedOrBalledAt = record.CreatedOrBalledAt,
            SourceType = record.CurrentStatus.ToString(),
            MustUseByDate = record.MustUseByDate,
            AgeDays = ageDays,
            OriginalBalls = record.QuantityBalls,
            UsedBalls = usedBalls,
            RemainingBalls = remainingBalls,
            CountsAsAvailable = record.CountsAsAvailable,
            IsReballCandidate = recommendation == DoughActionRecommendation.Reball,
            IsDiscardCandidate = recommendation == DoughActionRecommendation.Discard,
            RecommendedAction = recommendation.ToString()
        };
    }

    private static DoughSourceRemainingResponse MapLive(
        SourceProjectionState state,
        DateOnly referenceDate)
    {
        var record = state.Record;
        var ageDays = DoughQualityRules.CalculateOperationalAgeDays(record.CreatedOrBalledAt.ToLocalTime(), referenceDate);
        var remainingBalls = record.CountsAsAvailable
            ? Math.Max(state.ProjectedRemainingBalls, 0)
            : 0;
        var usedBalls = Math.Max(record.QuantityBalls - remainingBalls, 0);
        var recommendation = DetermineRecommendation(record, referenceDate, ageDays, remainingBalls);

        return new DoughSourceRemainingResponse
        {
            SourceDoughBatchQualityRecordId = record.Id,
            SourceDate = record.SourceDate,
            CreatedOrBalledAt = record.CreatedOrBalledAt,
            SourceType = record.CurrentStatus.ToString(),
            MustUseByDate = record.MustUseByDate,
            AgeDays = ageDays,
            OriginalBalls = record.QuantityBalls,
            UsedBalls = usedBalls,
            RemainingBalls = remainingBalls,
            CountsAsAvailable = record.CountsAsAvailable,
            IsReballCandidate = recommendation == DoughActionRecommendation.Reball,
            IsDiscardCandidate = recommendation == DoughActionRecommendation.Discard,
            RecommendedAction = recommendation.ToString()
        };
    }

    private static void ApplyCarryoverCap(
        IReadOnlyCollection<SourceProjectionState> states,
        DateOnly weekStartDate,
        Contracts.Responses.DoughClosing.WeeklyDoughCarryoverResponse carryover)
    {
        if (!carryover.HasClosingCarryover)
        {
            return;
        }

        var carryoverSources = states
            .Where(state => state.CreatedDate < weekStartDate && state.IsAvailableOn(weekStartDate))
            .OrderBy(state => GetConsumptionPriority(state.Record))
            .ThenBy(state => state.Record.SourceDate)
            .ThenBy(state => state.Record.CreatedOrBalledAt)
            .ToArray();

        var carryoverTotalBalls = carryoverSources.Sum(state => state.ProjectedRemainingBalls);
        var excessCarryoverBalls = Math.Max(carryoverTotalBalls - carryover.CarryoverAvailableBalls, 0);
        AllocateBalls(carryoverSources, excessCarryoverBalls);
    }

    private static void ApplyCurrentWeekUsage(
        IReadOnlyCollection<SourceProjectionState> states,
        DateOnly weekStartDate,
        DateOnly referenceDate,
        IReadOnlyList<DailyDoughClosing> closings,
        IReadOnlyList<DoughUsageTrace> traces)
    {
        var tracesByDate = traces
            .Where(trace => trace.UsageDate >= weekStartDate && trace.UsageDate <= referenceDate)
            .GroupBy(trace => trace.UsageDate)
            .ToDictionary(group => group.Key, group => group.OrderBy(trace => trace.CreatedAtUtc).ToArray());
        var closingsByDate = closings
            .Where(closing => closing.ClosingDate >= weekStartDate && closing.ClosingDate <= referenceDate)
            .ToDictionary(closing => closing.ClosingDate);

        for (var day = weekStartDate; day <= referenceDate; day = day.AddDays(1))
        {
            if (tracesByDate.TryGetValue(day, out var tracesForDay))
            {
                foreach (var trace in tracesForDay)
                {
                    var state = states.FirstOrDefault(item => item.Record.Id == trace.SourceDoughBatchQualityRecordId);
                    if (state is null)
                    {
                        continue;
                    }

                    state.ProjectedRemainingBalls = Math.Max(state.ProjectedRemainingBalls - trace.BallsUsed, 0);
                }
            }

            if (closingsByDate.TryGetValue(day, out var closing))
            {
                var tracedBallsForDay = tracesForDay?.Sum(trace => trace.BallsUsed) ?? 0;
                var untracedClosedDayBalls = Math.Max(closing.ActualUsedBalls - tracedBallsForDay, 0);

                if (untracedClosedDayBalls > 0)
                {
                    var liveSourcesForDay = states
                        .Where(state => state.IsAvailableOn(day) && state.ProjectedRemainingBalls > 0)
                        .OrderBy(state => GetConsumptionPriority(state.Record))
                        .ThenBy(state => state.Record.SourceDate)
                        .ThenBy(state => state.Record.CreatedOrBalledAt)
                        .ToArray();

                    AllocateBalls(liveSourcesForDay, untracedClosedDayBalls);
                }
            }

            foreach (var state in states)
            {
                state.ApplyEndOfDayState(day);
            }
        }
    }

    private static void AllocateBalls(
        IReadOnlyList<SourceProjectionState> states,
        int ballsToAllocate)
    {
        var remainingToAllocate = ballsToAllocate;

        foreach (var state in states)
        {
            if (remainingToAllocate <= 0)
            {
                break;
            }

            if (state.ProjectedRemainingBalls <= 0)
            {
                continue;
            }

            var appliedBalls = Math.Min(state.ProjectedRemainingBalls, remainingToAllocate);
            state.ProjectedRemainingBalls -= appliedBalls;
            remainingToAllocate -= appliedBalls;
        }
    }

    private static int GetConsumptionPriority(DoughBatchQualityRecord record)
    {
        return record.CurrentStatus switch
        {
            DoughQualityStatus.MustUseNextDay => 0,
            DoughQualityStatus.Reballed => 1,
            DoughQualityStatus.Attention => 2,
            _ => 3
        };
    }

    private static DoughActionRecommendation DetermineRecommendation(
        DoughBatchQualityRecord record,
        DateOnly referenceDate,
        int ageDays,
        int remainingBalls)
    {
        if (!record.CountsAsAvailable || remainingBalls <= 0)
        {
            return DoughActionRecommendation.None;
        }

        if (record.CurrentStatus == DoughQualityStatus.MustUseNextDay)
        {
            return record.MustUseByDate.HasValue && referenceDate > record.MustUseByDate.Value
                ? DoughActionRecommendation.Discard
                : DoughActionRecommendation.UseFirst;
        }

        if (record.CurrentStatus == DoughQualityStatus.Reballed)
        {
            return DoughActionRecommendation.UseFirst;
        }

        if (ageDays > DoughQualityRules.AttentionCandidatePreferredMaximumDays + 2)
        {
            return DoughActionRecommendation.Discard;
        }

        if (ageDays > DoughQualityRules.AttentionCandidatePreferredMaximumDays)
        {
            return DoughActionRecommendation.Reball;
        }

        if (record.CurrentStatus == DoughQualityStatus.Attention ||
            ageDays >= DoughQualityRules.AttentionCandidateMinimumDays)
        {
            return DoughActionRecommendation.Review;
        }

        return DoughActionRecommendation.None;
    }

    private static DateOnly GetOperationalWeekStart(DateOnly referenceDate)
    {
        var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
        return referenceDate.AddDays(-diff);
    }

    private sealed class SourceProjectionState
    {
        private readonly DateOnly? _discardDate;
        private readonly DateOnly? _partialReballDate;

        public SourceProjectionState(DoughBatchQualityRecord record, DateOnly weekStartDate)
        {
            Record = record;
            CreatedDate = DateOnly.FromDateTime(record.CreatedOrBalledAt.ToLocalTime());
            _discardDate = record.DiscardedAt.HasValue
                ? DateOnly.FromDateTime(record.DiscardedAt.Value.ToLocalTime())
                : null;
            _partialReballDate = record.ReballRecords
                .Where(reball => reball.Result == ReballResult.PartialRecovered)
                .OrderByDescending(reball => reball.ReballDate)
                .Select(reball => (DateOnly?)reball.ReballDate)
                .FirstOrDefault();

            var latestPartialReball = record.ReballRecords
                .Where(reball => reball.Result == ReballResult.PartialRecovered)
                .OrderByDescending(reball => reball.ReballDate)
                .FirstOrDefault();

            ProjectedRemainingBalls = ResolveWeekStartQuantity(record, weekStartDate, CreatedDate, _discardDate, latestPartialReball);
        }

        public DoughBatchQualityRecord Record { get; }

        public DateOnly CreatedDate { get; }

        public int ProjectedRemainingBalls { get; set; }

        public bool IsAvailableOn(DateOnly day)
        {
            if (day < CreatedDate)
            {
                return false;
            }

            return !_discardDate.HasValue || day <= _discardDate.Value;
        }

        public void ApplyEndOfDayState(DateOnly day)
        {
            if (_partialReballDate.HasValue && day >= _partialReballDate.Value)
            {
                ProjectedRemainingBalls = Math.Min(ProjectedRemainingBalls, Record.QuantityBalls);
            }

            if (_discardDate.HasValue && day >= _discardDate.Value)
            {
                ProjectedRemainingBalls = 0;
            }
        }

        private static int ResolveWeekStartQuantity(
            DoughBatchQualityRecord record,
            DateOnly weekStartDate,
            DateOnly createdDate,
            DateOnly? discardDate,
            DoughReballRecord? latestPartialReball)
        {
            if (discardDate.HasValue && discardDate.Value < weekStartDate)
            {
                return 0;
            }

            if (createdDate > weekStartDate)
            {
                return record.QuantityBalls;
            }

            if (latestPartialReball is not null && latestPartialReball.ReballDate >= weekStartDate)
            {
                return latestPartialReball.QuantityBeforeReball;
            }

            return record.QuantityBalls;
        }
    }
}
