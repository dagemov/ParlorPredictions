using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Application.Services.Dough;
using ParlorPrediction.Application.Services.Prep;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class DoughRegistrationFlowTests
{
    [Fact]
    public void Test1_WeeklyCarryover432_WithRuntimeSnapshot192_UsesCarryoverUnlessCorrected()
    {
        var ready = DoughWeeklyInventoryCalculator.ResolveCarryoverAnchoredReadyBalls(
            snapshotReadyBalls: 192,
            carryoverAvailableBalls: 432,
            hasClosingCarryover: true,
            hasCurrentWeekSnapshot: true,
            producedThisWeekBalls: 0,
            actualUsedBallsThisWeek: 0);

        Assert.Equal(432, ready);
    }

    [Fact]
    public async Task Test2_MakeDoughLoadCompleted_ReadyUnchanged_MixedIncreasesBy168()
    {
        var tuesday = new DateOnly(2026, 6, 9);
        var fixture = CreateCalendarFixture(tuesday);
        SetCarryover(fixture, carryoverReady: 432);
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(tuesday, 432));

        fixture.Batches.Batches.Add(new DoughBatch(
            Guid.NewGuid(),
            batchDate: tuesday,
            totalCases: DoughBatch.StandardLoadCases));

        var result = await fixture.Service.GetWeekAsync(tuesday, historicalWeeksToUse: 8);

        Assert.Equal(432, result.ReadyNowBalls);
        Assert.Equal(168, result.MixedButNotBalledBalls);
        Assert.Equal(1, result.MixedButNotBalledLoads);
        Assert.Equal(0, result.ProducedThisWeekBalls);
    }

    [Fact]
    public async Task Test3_BallDoughCompleted_ReadyIncreasesBy168_MixedReturnsToZero()
    {
        var tuesday = new DateOnly(2026, 6, 9);
        var fixture = CreateCalendarFixture(tuesday);
        SetCarryover(fixture, carryoverReady: 432);
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(tuesday, 432));

        var batch = new DoughBatch(
            Guid.NewGuid(),
            batchDate: tuesday,
            totalCases: DoughBatch.StandardLoadCases);
        batch.MarkAsBalled(new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc));
        fixture.Batches.Batches.Add(batch);

        fixture.Tasks.Tasks.Add(CreateCompletedBallTask(
            tuesday,
            new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc),
            168));

        var result = await fixture.Service.GetWeekAsync(tuesday, historicalWeeksToUse: 8);

        Assert.Equal(600, result.ReadyNowBalls);
        Assert.Equal(0, result.MixedButNotBalledBalls);
        Assert.Equal(0, result.MixedButNotBalledLoads);
        Assert.Equal(168, result.ProducedThisWeekBalls);
        Assert.Equal(0, result.StillFermentingBalls);
    }

    [Fact]
    public async Task Test4_DailyClosingActualUsed45_ReadyDecreasesBy45_VarianceDoesNotChangeProduction()
    {
        var tuesday = new DateOnly(2026, 6, 9);
        var fixture = CreateCalendarFixture(tuesday, useCanonicalWeekNeed: true);
        SetCarryover(fixture, carryoverReady: 432);
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(tuesday, 192));

        var batch = new DoughBatch(
            Guid.NewGuid(),
            batchDate: tuesday,
            totalCases: DoughBatch.StandardLoadCases);
        batch.MarkAsBalled(new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc));
        fixture.Batches.Batches.Add(batch);

        fixture.Tasks.Tasks.Add(CreateCompletedBallTask(
            tuesday,
            new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc),
            168));

        fixture.DailyClosings.Items.Add(DailyDoughClosing.Create(
            tuesday,
            tuesday,
            forecastNeededBalls: 90,
            actualUsedBalls: 45,
            closedByUserId: "manager-user",
            closedAtUtc: DateTime.UtcNow));

        var result = await fixture.Service.GetWeekAsync(tuesday, historicalWeeksToUse: 8);

        Assert.Equal(555, result.ReadyNowBalls);
        Assert.Equal(45, result.ActualUsedBallsThisWeek);
        Assert.Equal(45, result.AccumulatedDailyVariance);
        Assert.Equal(168, result.ProducedThisWeekBalls);
    }

    [Fact]
    public async Task Test5_StillFermentingAlreadyInReady_StillMissingDoesNotSubtract168Twice()
    {
        var tuesday = new DateOnly(2026, 6, 9);
        var fixture = CreateCalendarFixture(tuesday, useCanonicalWeekNeed: true);
        SetCarryover(fixture, carryoverReady: 432);
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(tuesday, 192));

        var batch = new DoughBatch(
            Guid.NewGuid(),
            batchDate: tuesday,
            totalCases: DoughBatch.StandardLoadCases);
        batch.MarkAsBalled(new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc));
        fixture.Batches.Batches.Add(batch);

        fixture.Tasks.Tasks.Add(CreateCompletedBallTask(
            tuesday,
            new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc),
            168));

        fixture.DailyClosings.Items.Add(DailyDoughClosing.Create(
            tuesday,
            tuesday,
            forecastNeededBalls: 90,
            actualUsedBalls: 45,
            closedByUserId: "manager-user",
            closedAtUtc: DateTime.UtcNow));

        var result = await fixture.Service.GetWeekAsync(tuesday, historicalWeeksToUse: 8);

        Assert.Equal(1063, result.WeekTotalNeededBalls);
        Assert.Equal(555, result.ReadyNowBalls);
        Assert.Equal(0, result.MixedButNotBalledBalls);
        Assert.Equal(0, result.StillFermentingBalls);
        Assert.Equal(463, result.StillMissingThisWeekBalls);
    }

    [Fact]
    public async Task ExampleA_WeeklyNeed1063_With432ReadyAnd168Mixed_StillMissing463()
    {
        var tuesday = new DateOnly(2026, 6, 9);
        var fixture = CreateCalendarFixture(tuesday, useCanonicalWeekNeed: true);
        SetCarryover(fixture, carryoverReady: 432);
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(tuesday, 432));

        fixture.Batches.Batches.Add(new DoughBatch(
            Guid.NewGuid(),
            batchDate: tuesday,
            totalCases: DoughBatch.StandardLoadCases));

        var result = await fixture.Service.GetWeekAsync(tuesday, historicalWeeksToUse: 8);

        Assert.Equal(432, result.ReadyNowBalls);
        Assert.Equal(168, result.MixedButNotBalledBalls);
        Assert.Equal(463, result.StillMissingThisWeekBalls);
    }

    private static void SetCarryover(CalendarFixture fixture, int carryoverReady)
    {
        fixture.WeeklyClosingRead.Carryover = new WeeklyDoughCarryoverResponse
        {
            HasClosingCarryover = true,
            SourceWeekStartDate = new DateOnly(2026, 6, 2),
            SourceWeekEndDate = new DateOnly(2026, 6, 7),
            CarryoverReadyBalls = carryoverReady,
            CarryoverAttentionBalls = 0,
            CarryoverAvailableBalls = carryoverReady,
            MixedButNotBalledLoads = 0
        };
    }

    private static CalendarFixture CreateCalendarFixture(DateOnly referenceDate, bool useCanonicalWeekNeed = false)
    {
        IDoughPrepCalculationService calculationService = useCanonicalWeekNeed
            ? new CanonicalWeekCalculationService()
            : new FixedWeeklyCalculationService(referenceDate);

        var batches = new InMemoryDoughBatchReadRepository();
        var inventorySnapshots = new InMemoryDoughInventoryReadRepository();
        var tasks = new InMemoryPrepTaskRepository();
        var weeklyClosingRead = new StubWeeklyDoughClosingReadService();
        var dailyClosings = new InMemoryDailyDoughClosingRepository();
        var qualityRecords = new InMemoryDoughBatchQualityRepository();
        var lossRecords = new InMemoryDoughLossRecordRepository();
        var usageTraces = new InMemoryDoughUsageTraceRepository();
        var sourceProjectionService = new DoughSourceProjectionService(
            qualityRecords,
            dailyClosings,
            usageTraces,
            weeklyClosingRead);
        var availabilityProjectionService = new DoughAvailabilityProjectionService(
            dailyClosings,
            sourceProjectionService,
            usageTraces,
            inventorySnapshots,
            lossRecords,
            tasks,
            weeklyClosingRead);

        return new CalendarFixture(
            batches,
            inventorySnapshots,
            tasks,
            weeklyClosingRead,
            dailyClosings,
            qualityRecords,
            lossRecords,
            new PrepWeeklyDoughCalendarService(
                availabilityProjectionService,
                calculationService,
                batches,
                inventorySnapshots,
                dailyClosings,
                tasks));
    }

    private static DoughInventorySnapshot CreateSnapshot(DateOnly snapshotDate, int availableBalls)
    {
        return new DoughInventorySnapshot(
            Guid.NewGuid(),
            snapshotDate,
            availableBalls,
            newBalls: availableBalls,
            oldBalls: 0,
            reservedBalls: 0,
            usedBalls: 0,
            wasteBalls: 0);
    }

    private static PrepTask CreateCompletedBallTask(
        DateOnly taskDate,
        DateTime completedAtUtc,
        int quantityCompleted)
    {
        var task = PrepTask.Create(
            taskDate,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ApplicationRole.PizzaMaker,
            quantityRecommended: quantityCompleted,
            taskType: PrepTaskType.BallDough,
            quantityUnit: DoughQuantityUnit.Balls);

        task.Complete("user-1", quantityCompleted, completedAtUtc: completedAtUtc);
        return task;
    }

    private sealed record CalendarFixture(
        InMemoryDoughBatchReadRepository Batches,
        InMemoryDoughInventoryReadRepository InventorySnapshots,
        InMemoryPrepTaskRepository Tasks,
        StubWeeklyDoughClosingReadService WeeklyClosingRead,
        InMemoryDailyDoughClosingRepository DailyClosings,
        InMemoryDoughBatchQualityRepository QualityRecords,
        InMemoryDoughLossRecordRepository LossRecords,
        PrepWeeklyDoughCalendarService Service);

    private sealed class CanonicalWeekCalculationService : IDoughPrepCalculationService
    {
        private static readonly int[] DailyRequiredBalls = [177, 177, 177, 177, 177, 178];

        public Task<DoughPrepCalculationResult> CalculateAsync(
            CalculateDoughPrepRequest request,
            CancellationToken cancellationToken = default)
        {
            var weekStart = GetOperationalWeekStart(request.TargetDate);
            var offset = request.TargetDate.DayNumber - weekStart.DayNumber;
            var requiredBalls = offset >= 0 && offset < DailyRequiredBalls.Length
                ? DailyRequiredBalls[offset]
                : 0;

            return Task.FromResult(new DoughPrepCalculationResult
            {
                TargetDate = request.TargetDate,
                HistoricalAverageBalls = requiredBalls,
                EventEstimatedBalls = 0,
                RequiredBalls = requiredBalls,
                AvailableBalls = 0,
                CompletedBalls = 0,
                MissingBalls = requiredBalls,
                RecommendedCases = 0,
                RecommendedLoads = 0,
                Reason = "canonical-week-test"
            });
        }

        private static DateOnly GetOperationalWeekStart(DateOnly referenceDate)
        {
            var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
            return referenceDate.AddDays(-diff);
        }
    }

    private sealed class FixedWeeklyCalculationService : IDoughPrepCalculationService
    {
        private readonly DateOnly _referenceDate;

        public FixedWeeklyCalculationService(DateOnly referenceDate)
        {
            _referenceDate = referenceDate;
        }

        public Task<DoughPrepCalculationResult> CalculateAsync(
            CalculateDoughPrepRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DoughPrepCalculationResult
            {
                TargetDate = request.TargetDate,
                HistoricalAverageBalls = 100,
                EventEstimatedBalls = 0,
                RequiredBalls = 100,
                AvailableBalls = request.TargetDate == _referenceDate ? 24 : 0,
                CompletedBalls = 0,
                MissingBalls = 100,
                RecommendedCases = 0,
                RecommendedLoads = 0,
                Reason = "test"
            });
        }
    }

    private sealed class StubWeeklyDoughClosingReadService : IWeeklyDoughClosingReadService
    {
        public WeeklyDoughCarryoverResponse Carryover { get; set; } = new();

        public Task<IReadOnlyList<WeeklyDoughClosingResponse>> GetWeeklyClosingsAsync(
            GetWeeklyClosingsRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<WeeklyDoughClosingResponse>>(Array.Empty<WeeklyDoughClosingResponse>());
        }

        public Task<WeeklyDoughCarryoverResponse> GetCarryoverForWeekAsync(
            GetWeeklyDoughCarryoverRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Carryover);
        }
    }

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

        public Task<IReadOnlyList<DailyDoughClosing>> SearchAsync(
            DateOnly? closingDateFrom,
            DateOnly? closingDateTo,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<DailyDoughClosing> query = Items;

            if (closingDateFrom.HasValue)
            {
                query = query.Where(item => item.ClosingDate >= closingDateFrom.Value);
            }

            if (closingDateTo.HasValue)
            {
                query = query.Where(item => item.ClosingDate <= closingDateTo.Value);
            }

            return Task.FromResult<IReadOnlyList<DailyDoughClosing>>(
                query.OrderBy(item => item.ClosingDate).ToArray());
        }

        public Task<IReadOnlyList<DailyDoughClosing>> ListByWeekStartDateAsync(
            DateOnly weekStartDate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DailyDoughClosing>>(
                Items.Where(item => item.WeekStartDate == weekStartDate).OrderBy(item => item.ClosingDate).ToArray());
        }
    }

    private sealed class InMemoryDoughBatchReadRepository : IDoughBatchReadRepository
    {
        public List<DoughBatch> Batches { get; } = [];

        public Task<IReadOnlyCollection<DoughBatch>> GetProducedOnOrBeforeAsync(
            DateOnly productionDate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<DoughBatch>>(
                Batches.Where(batch => batch.BatchDate <= productionDate).ToArray());
        }
    }

    private sealed class InMemoryDoughInventoryReadRepository : IDoughInventoryReadRepository
    {
        public List<DoughInventorySnapshot> Snapshots { get; } = [];

        public Task<DoughInventorySnapshot?> GetLatestSnapshotOnOrBeforeAsync(
            DateOnly targetDate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DoughInventorySnapshot?>(
                Snapshots
                    .Where(snapshot => snapshot.SnapshotDate <= targetDate)
                    .OrderByDescending(snapshot => snapshot.SnapshotDate)
                    .ThenByDescending(snapshot => snapshot.UpdatedAtUtc)
                    .FirstOrDefault());
        }
    }

    private sealed class InMemoryPrepTaskRepository : IPrepTaskRepository
    {
        public List<PrepTask> Tasks { get; } = [];

        public Task AddAsync(PrepTask task, CancellationToken cancellationToken = default)
        {
            Tasks.Add(task);
            return Task.CompletedTask;
        }

        public Task<PrepTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PrepTask?>(Tasks.SingleOrDefault(task => task.Id == id));
        }

        public Task<PrepTask?> GetByDoughPrepRecommendationIdAsync(Guid doughPrepRecommendationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PrepTask?>(Tasks.SingleOrDefault(task => task.DoughPrepRecommendationId == doughPrepRecommendationId));
        }

        public Task<IReadOnlyList<PrepTask>> GetDoughTasksByDateAsync(DateOnly taskDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PrepTask>>(Tasks.Where(task => task.TaskDate == taskDate).ToArray());
        }

        public Task<IReadOnlyList<PrepTask>> GetDoughTasksBetweenDatesAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PrepTask>>(
                Tasks.Where(task => task.TaskDate >= startDate && task.TaskDate <= endDate).ToArray());
        }

        public Task<IReadOnlyList<PrepTask>> SearchDoughTasksAsync(
            DateOnly? taskDate,
            PrepTaskStatus? status,
            ApplicationRole? assignedRole,
            Guid? prepItemId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PrepTask>>(Tasks.ToArray());
        }

        public void Remove(PrepTask task)
        {
            Tasks.Remove(task);
        }
    }

    private sealed class InMemoryDoughBatchQualityRepository : IDoughBatchQualityRepository
    {
        public List<DoughBatchQualityRecord> Records { get; } = [];

        public Task AddAsync(DoughBatchQualityRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<DoughBatchQualityRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DoughBatchQualityRecord?>(Records.FirstOrDefault(record => record.Id == id));
        }

        public Task<IReadOnlyList<DoughBatchQualityRecord>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DoughBatchQualityRecord>>(Records.ToArray());
        }

        public Task<IReadOnlyList<DoughBatchQualityRecord>> SearchAsync(
            DateOnly? sourceDateFrom,
            DateOnly? sourceDateTo,
            DateOnly? createdOrBalledFromDate,
            DateOnly? createdOrBalledToDate,
            DateOnly? reballedFromDate,
            DateOnly? reballedToDate,
            DoughQualityStatus? currentStatus,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<DoughBatchQualityRecord> query = Records;

            if (currentStatus.HasValue)
            {
                query = query.Where(record => record.CurrentStatus == currentStatus.Value);
            }

            return Task.FromResult<IReadOnlyList<DoughBatchQualityRecord>>(query.ToArray());
        }
    }

    private sealed class InMemoryDoughLossRecordRepository : IDoughLossRecordRepository
    {
        public List<DoughLossRecord> Records { get; } = [];

        public Task AddAsync(DoughLossRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DoughLossRecord>> SearchAsync(
            DateOnly? fromDate,
            DateOnly? toDate,
            DoughLossReason? lossReason,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<DoughLossRecord> query = Records;

            if (fromDate.HasValue)
            {
                query = query.Where(record => record.LossDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(record => record.LossDate <= toDate.Value);
            }

            if (lossReason.HasValue)
            {
                query = query.Where(record => record.LossReason == lossReason.Value);
            }

            return Task.FromResult<IReadOnlyList<DoughLossRecord>>(query.ToArray());
        }
    }
}
