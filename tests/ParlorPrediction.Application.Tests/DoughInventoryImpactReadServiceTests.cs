using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Application.Services.Dough;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.DoughQuality;
using ParlorPrediction.Contracts.Requests.DoughUsage;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughQuality;
using ParlorPrediction.Contracts.Responses.DoughUsage;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Domain.Entities;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class DoughInventoryImpactReadServiceTests
{
    [Fact]
    public async Task GetInventoryImpactAsync_ShowsRemainingBySourceDate_AndUseFirstSourcesRiseToTheTop()
    {
        var fixture = CreateFixture();
        fixture.WeeklyCalendar.Response = CreateWeeklyCalendar(readyNowBalls: 720, mixedButNotBalledBalls: 168, stillMissingBalls: 223);
        fixture.UsageTraceRead.RemainingSources =
        [
            CreateSource(new DateOnly(2026, 6, 14), "Good", "None", remainingBalls: 72),
            CreateSource(new DateOnly(2026, 6, 13), "MustUseNextDay", "UseFirst", remainingBalls: 24),
            CreateSource(new DateOnly(2026, 6, 12), "Reballed", "UseFirst", remainingBalls: 24),
            CreateSource(new DateOnly(2026, 6, 11), "Attention", "Review", remainingBalls: 18)
        ];
        fixture.UsageTraceRead.TodayTraces =
        [
            new DoughUsageTraceResponse
            {
                Id = Guid.NewGuid(),
                UsageDate = new DateOnly(2026, 6, 17),
                BallsUsed = 36
            }
        ];
        fixture.QualityRead.LossAnalytics = new DoughLossAnalyticsResponse
        {
            TotalLostBalls = 12
        };

        var response = await fixture.Service.GetInventoryImpactAsync(new GetDoughInventoryImpactRequest
        {
            ReferenceDate = new DateOnly(2026, 6, 17),
            HistoricalWeeksToUse = 8
        });

        Assert.Equal(48, response.UseFirstBalls);
        Assert.Equal(18, response.AttentionBalls);
        Assert.Equal(36, response.UsedTodayBalls);
        Assert.Equal(12, response.LostOrDiscardedBalls);
        Assert.Equal(new DateOnly(2026, 6, 12), response.RemainingSources[0].SourceDate);
        Assert.Equal("UseFirst", response.RemainingSources[0].RecommendedAction);
        Assert.Equal("MustUseNextDay", response.RemainingSources[1].SourceType);
        Assert.Equal("UseFirst", response.RemainingSources[1].RecommendedAction);
    }

    [Fact]
    public async Task GetInventoryImpactAsync_UsesDailyClosingTotalForClosedDayUsage()
    {
        var fixture = CreateFixture();
        fixture.WeeklyCalendar.Response = CreateWeeklyCalendar(readyNowBalls: 720, mixedButNotBalledBalls: 168, stillMissingBalls: 223);
        fixture.UsageTraceRead.RemainingSources =
        [
            CreateSource(new DateOnly(2026, 6, 14), "Good", "None", remainingBalls: 72)
        ];
        fixture.UsageTraceRead.TodayTraces =
        [
            new DoughUsageTraceResponse
            {
                Id = Guid.NewGuid(),
                UsageDate = new DateOnly(2026, 6, 17),
                BallsUsed = 36
            }
        ];
        fixture.DailyClosings.Items.Add(DailyDoughClosing.Create(
            closingDate: new DateOnly(2026, 6, 17),
            weekStartDate: new DateOnly(2026, 6, 16),
            forecastNeededBalls: 90,
            actualUsedBalls: 60,
            closedByUserId: "manager-user",
            closedAtUtc: DateTime.UtcNow));

        var response = await fixture.Service.GetInventoryImpactAsync(new GetDoughInventoryImpactRequest
        {
            ReferenceDate = new DateOnly(2026, 6, 17),
            HistoricalWeeksToUse = 8
        });

        Assert.Equal(60, response.UsedTodayBalls);
    }

    [Fact]
    public async Task GetInventoryImpactAsync_KeepsMixedButNotBalledSeparateFromReadyNow()
    {
        var fixture = CreateFixture();
        fixture.WeeklyCalendar.Response = CreateWeeklyCalendar(readyNowBalls: 720, mixedButNotBalledBalls: 168, stillMissingBalls: 223);
        fixture.UsageTraceRead.RemainingSources =
        [
            CreateSource(new DateOnly(2026, 6, 14), "Good", "None", remainingBalls: 672),
            CreateSource(new DateOnly(2026, 6, 14), "Reballed", "UseFirst", remainingBalls: 48)
        ];

        var response = await fixture.Service.GetInventoryImpactAsync(new GetDoughInventoryImpactRequest
        {
            ReferenceDate = new DateOnly(2026, 6, 17),
            HistoricalWeeksToUse = 8
        });

        Assert.Equal(720, response.ReadyNowBalls);
        Assert.Equal(168, response.MixedButNotBalledBalls);
        Assert.Equal(168, response.FutureBalls);
        Assert.Equal(223, response.StillMissingBalls);
    }

    private static Fixture CreateFixture()
    {
        var dailyClosings = new InMemoryDailyDoughClosingRepository();
        var qualityRead = new StubDoughQualityReadService();
        var usageTraceRead = new StubDoughUsageTraceReadService();
        var weeklyCalendar = new StubPrepWeeklyDoughCalendarService();

        return new Fixture(
            dailyClosings,
            qualityRead,
            usageTraceRead,
            weeklyCalendar,
            new DoughInventoryImpactReadService(
                dailyClosings,
                qualityRead,
                usageTraceRead,
                weeklyCalendar));
    }

    private static WeeklyDoughCalendarResponse CreateWeeklyCalendar(
        int readyNowBalls,
        int mixedButNotBalledBalls,
        int stillMissingBalls)
    {
        return new WeeklyDoughCalendarResponse
        {
            WeekStartDate = new DateOnly(2026, 6, 16),
            WeekEndDate = new DateOnly(2026, 6, 21),
            WeekTotalNeededBalls = 943,
            ReadyNowBalls = readyNowBalls,
            MixedButNotBalledBalls = mixedButNotBalledBalls,
            MixedButNotBalledLoads = mixedButNotBalledBalls <= 0 ? 0 : 1,
            FutureBalls = mixedButNotBalledBalls,
            StillMissingThisWeekBalls = stillMissingBalls
        };
    }

    private static DoughSourceRemainingResponse CreateSource(
        DateOnly sourceDate,
        string sourceType,
        string recommendedAction,
        int remainingBalls)
    {
        return new DoughSourceRemainingResponse
        {
            SourceDoughBatchQualityRecordId = Guid.NewGuid(),
            SourceDate = sourceDate,
            CreatedOrBalledAt = sourceDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(10))),
            SourceType = sourceType,
            OriginalBalls = remainingBalls,
            RemainingBalls = remainingBalls,
            CountsAsAvailable = true,
            RecommendedAction = recommendedAction
        };
    }

    private sealed record Fixture(
        InMemoryDailyDoughClosingRepository DailyClosings,
        StubDoughQualityReadService QualityRead,
        StubDoughUsageTraceReadService UsageTraceRead,
        StubPrepWeeklyDoughCalendarService WeeklyCalendar,
        DoughInventoryImpactReadService Service);

    private sealed class InMemoryDailyDoughClosingRepository : IDailyDoughClosingRepository
    {
        public List<DailyDoughClosing> Items { get; } = [];

        public Task AddAsync(DailyDoughClosing closing, CancellationToken cancellationToken = default)
        {
            Items.Add(closing);
            return Task.CompletedTask;
        }

        public Task<DailyDoughClosing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.FirstOrDefault(item => item.Id == id));
        }

        public Task<DailyDoughClosing?> GetByClosingDateAsync(DateOnly closingDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.FirstOrDefault(item => item.ClosingDate == closingDate));
        }

        public Task<IReadOnlyList<DailyDoughClosing>> SearchAsync(DateOnly? closingDateFrom, DateOnly? closingDateTo, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DailyDoughClosing>>(Items.ToArray());
        }

        public Task<IReadOnlyList<DailyDoughClosing>> ListByWeekStartDateAsync(DateOnly weekStartDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DailyDoughClosing>>(Items.Where(item => item.WeekStartDate == weekStartDate).ToArray());
        }
    }

    private sealed class StubDoughQualityReadService : IDoughQualityReadService
    {
        public DoughLossAnalyticsResponse LossAnalytics { get; set; } = new();

        public Task<IReadOnlyList<Contracts.Responses.DoughQuality.DoughBatchQualityRecordResponse>> SearchAsync(Contracts.Requests.DoughQuality.SearchDoughBatchQualityRecordsRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Contracts.Responses.DoughQuality.DoughBatchQualityRecordResponse>>(Array.Empty<Contracts.Responses.DoughQuality.DoughBatchQualityRecordResponse>());
        }

        public Task<IReadOnlyList<Contracts.Responses.DoughQuality.DoughAttentionCandidateResponse>> EvaluateAttentionCandidatesAsync(Contracts.Requests.DoughQuality.EvaluateDoughAttentionCandidatesRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Contracts.Responses.DoughQuality.DoughAttentionCandidateResponse>>(Array.Empty<Contracts.Responses.DoughQuality.DoughAttentionCandidateResponse>());
        }

        public Task<DoughQualitySummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DoughQualitySummaryResponse());
        }

        public Task<DoughLossAnalyticsResponse> GetLossAnalyticsAsync(GetDoughLossAnalyticsRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(LossAnalytics);
        }
    }

    private sealed class StubDoughUsageTraceReadService : IDoughUsageTraceReadService
    {
        public IReadOnlyList<DoughSourceRemainingResponse> RemainingSources { get; set; } = Array.Empty<DoughSourceRemainingResponse>();

        public IReadOnlyList<DoughUsageTraceResponse> TodayTraces { get; set; } = Array.Empty<DoughUsageTraceResponse>();

        public Task<DoughUsageTraceResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DoughUsageTraceResponse?>(null);
        }

        public Task<IReadOnlyList<DoughUsageTraceResponse>> SearchAsync(SearchDoughUsageTracesRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(TodayTraces);
        }

        public Task<IReadOnlyList<DoughUsageSourceOptionResponse>> GetAvailableSourcesForDateAsync(GetAvailableDoughSourcesRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DoughUsageSourceOptionResponse>>(Array.Empty<DoughUsageSourceOptionResponse>());
        }

        public Task<IReadOnlyList<DoughSourceRemainingResponse>> GetRemainingBySourceAsync(GetDoughRemainingBySourceRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RemainingSources);
        }

        public Task<DoughReballPlanningResponse> GetReballPlanningForDateAsync(GetDoughReballPlanningRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DoughReballPlanningResponse());
        }
    }

    private sealed class StubPrepWeeklyDoughCalendarService : IPrepWeeklyDoughCalendarService
    {
        public WeeklyDoughCalendarResponse Response { get; set; } = new();

        public Task<WeeklyDoughCalendarResponse> GetWeekAsync(DateOnly referenceDate, int historicalWeeksToUse = 8, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Response);
        }
    }
}
