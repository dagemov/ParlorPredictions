using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.DoughQuality;
using ParlorPrediction.Contracts.Requests.DoughUsage;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DoughInventoryImpactReadService : IDoughInventoryImpactReadService
{
    private readonly IDailyDoughClosingRepository _dailyDoughClosingRepository;
    private readonly IDoughQualityReadService _doughQualityReadService;
    private readonly IDoughUsageTraceReadService _doughUsageTraceReadService;
    private readonly IPrepWeeklyDoughCalendarService _prepWeeklyDoughCalendarService;

    public DoughInventoryImpactReadService(
        IDailyDoughClosingRepository dailyDoughClosingRepository,
        IDoughQualityReadService doughQualityReadService,
        IDoughUsageTraceReadService doughUsageTraceReadService,
        IPrepWeeklyDoughCalendarService prepWeeklyDoughCalendarService)
    {
        _dailyDoughClosingRepository = dailyDoughClosingRepository;
        _doughQualityReadService = doughQualityReadService;
        _doughUsageTraceReadService = doughUsageTraceReadService;
        _prepWeeklyDoughCalendarService = prepWeeklyDoughCalendarService;
    }

    public async Task<DoughInventoryImpactResponse> GetInventoryImpactAsync(
        GetDoughInventoryImpactRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ReferenceDate == default)
        {
            throw new ArgumentException("Reference date is required.", nameof(request));
        }

        var historicalWeeksToUse = request.HistoricalWeeksToUse < 1
            ? 8
            : request.HistoricalWeeksToUse;
        var weeklyCalendar = await _prepWeeklyDoughCalendarService.GetWeekAsync(
            request.ReferenceDate,
            historicalWeeksToUse,
            cancellationToken);
        var remainingBySource = await _doughUsageTraceReadService.GetRemainingBySourceAsync(
            new GetDoughRemainingBySourceRequest
            {
                ReferenceDate = request.ReferenceDate
            },
            cancellationToken);
        var todayClosing = await _dailyDoughClosingRepository.GetByClosingDateAsync(
            request.ReferenceDate,
            cancellationToken);
        var todayTraces = await _doughUsageTraceReadService.SearchAsync(
            new SearchDoughUsageTracesRequest
            {
                UsageDateFrom = request.ReferenceDate,
                UsageDateTo = request.ReferenceDate
            },
            cancellationToken);
        var lossAnalytics = await _doughQualityReadService.GetLossAnalyticsAsync(
            new GetDoughLossAnalyticsRequest
            {
                FromDate = weeklyCalendar.WeekStartDate,
                ToDate = request.ReferenceDate
            },
            cancellationToken);

        var activeSources = remainingBySource
            .Where(source => source.CountsAsAvailable && source.RemainingBalls > 0)
            .OrderBy(GetActionPriority)
            .ThenBy(source => source.SourceDate)
            .ThenBy(source => source.CreatedOrBalledAt)
            .ToArray();

        return new DoughInventoryImpactResponse
        {
            ReferenceDate = request.ReferenceDate,
            WeekStartDate = weeklyCalendar.WeekStartDate,
            WeekEndDate = weeklyCalendar.WeekEndDate,
            WeeklyGoalBalls = weeklyCalendar.WeekTotalNeededBalls,
            ReadyNowBalls = weeklyCalendar.ReadyNowBalls,
            StillMissingBalls = weeklyCalendar.StillMissingThisWeekBalls,
            UseFirstBalls = SumByRecommendedAction(activeSources, DoughActionRecommendation.UseFirst),
            AttentionBalls = SumByStatus(activeSources, DoughQualityStatus.Attention),
            MixedButNotBalledBalls = weeklyCalendar.MixedButNotBalledBalls,
            FutureBalls = weeklyCalendar.FutureBalls,
            UsedTodayBalls = todayClosing?.ActualUsedBalls ?? todayTraces.Sum(trace => trace.BallsUsed),
            LostOrDiscardedBalls = lossAnalytics.TotalLostBalls,
            RemainingTrackedBalls = activeSources.Sum(source => source.RemainingBalls),
            RemainingSources = activeSources
                .Select(source => new DoughInventoryImpactSourceResponse
                {
                    SourceDoughBatchQualityRecordId = source.SourceDoughBatchQualityRecordId,
                    SourceDate = source.SourceDate,
                    CreatedOrBalledAt = source.CreatedOrBalledAt,
                    SourceType = source.SourceType,
                    MustUseByDate = source.MustUseByDate,
                    AgeDays = source.AgeDays,
                    OriginalBalls = source.OriginalBalls,
                    UsedBalls = source.UsedBalls,
                    RemainingBalls = source.RemainingBalls,
                    CountsAsAvailable = source.CountsAsAvailable,
                    IsReballCandidate = source.IsReballCandidate,
                    IsDiscardCandidate = source.IsDiscardCandidate,
                    RecommendedAction = source.RecommendedAction
                })
                .ToArray()
        };
    }

    private static int SumByRecommendedAction(
        IReadOnlyCollection<Contracts.Responses.DoughUsage.DoughSourceRemainingResponse> sources,
        DoughActionRecommendation action)
    {
        return sources
            .Where(source => string.Equals(source.RecommendedAction, action.ToString(), StringComparison.OrdinalIgnoreCase))
            .Sum(source => source.RemainingBalls);
    }

    private static int SumByStatus(
        IReadOnlyCollection<Contracts.Responses.DoughUsage.DoughSourceRemainingResponse> sources,
        DoughQualityStatus status)
    {
        return sources
            .Where(source => string.Equals(source.SourceType, status.ToString(), StringComparison.OrdinalIgnoreCase))
            .Sum(source => source.RemainingBalls);
    }

    private static int GetActionPriority(Contracts.Responses.DoughUsage.DoughSourceRemainingResponse source)
    {
        if (string.Equals(source.RecommendedAction, DoughActionRecommendation.UseFirst.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(source.RecommendedAction, DoughActionRecommendation.Review.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(source.RecommendedAction, DoughActionRecommendation.Reball.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(source.RecommendedAction, DoughActionRecommendation.Discard.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        return 4;
    }
}
