using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Application.Services.Dough;
using ParlorPrediction.Application.Services.Prep;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class PrepWeeklyDoughCalendarServiceTests
{
    [Fact]
    public async Task Completed_Tasks_Before_Planning_Window_Are_Not_Counted_As_Finished_This_Week()
    {
        var fixture = CreateFixture(referenceDate: new DateOnly(2026, 6, 12));
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(
            snapshotDate: new DateOnly(2026, 6, 12),
            availableBalls: 24));

        fixture.Tasks.Tasks.Add(CreateCompletedTask(
            taskDate: new DateOnly(2026, 6, 10),
            completedAtUtc: new DateTime(2026, 6, 10, 16, 0, 0, DateTimeKind.Utc),
            quantityCompleted: 84));

        fixture.Tasks.Tasks.Add(CreateCompletedTask(
            taskDate: new DateOnly(2026, 6, 8),
            completedAtUtc: new DateTime(2026, 6, 8, 16, 0, 0, DateTimeKind.Utc),
            quantityCompleted: 168));

        var result = await fixture.Service.GetWeekAsync(
            new DateOnly(2026, 6, 12),
            historicalWeeksToUse: 8);

        Assert.Equal(84, result.FinishedThisWeekBalls);
        Assert.Equal(168, result.PreviousWeekFinishedBalls);
    }

    [Fact]
    public async Task Previous_Week_Used_Or_Finished_Is_Displayed_Separately()
    {
        var fixture = CreateFixture(referenceDate: new DateOnly(2026, 6, 9));
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(
            snapshotDate: new DateOnly(2026, 6, 9),
            availableBalls: 24));

        fixture.Tasks.Tasks.Add(CreateCompletedTask(
            taskDate: new DateOnly(2026, 6, 7),
            completedAtUtc: new DateTime(2026, 6, 7, 16, 0, 0, DateTimeKind.Utc),
            quantityCompleted: 168));

        var result = await fixture.Service.GetWeekAsync(
            new DateOnly(2026, 6, 9),
            historicalWeeksToUse: 8);

        Assert.Equal(168, result.PreviousWeekFinishedBalls);
        Assert.Equal(0, result.FinishedThisWeekBalls);
        Assert.Equal(576, result.StillMissingThisWeekBalls);
    }

    [Fact]
    public async Task Carryover_Available_Is_Counted_Only_If_Still_Available()
    {
        var fixture = CreateFixture(referenceDate: new DateOnly(2026, 6, 9));
        fixture.InventorySnapshots.Snapshots.Add(new DoughInventorySnapshot(
            Guid.NewGuid(),
            new DateOnly(2026, 6, 9),
            availableBalls: 24,
            newBalls: 0,
            oldBalls: 24,
            reservedBalls: 0,
            usedBalls: 700,
            wasteBalls: 20));

        var result = await fixture.Service.GetWeekAsync(
            new DateOnly(2026, 6, 9),
            historicalWeeksToUse: 8);

        Assert.Equal(24, result.ReadyNowBalls);
        Assert.Equal(576, result.StillMissingThisWeekBalls);
    }

    [Fact]
    public async Task Weekly_Closing_With_300_Leftover_Balls_And_One_Mixed_Load_Carries_300_Available_And_168_Potential()
    {
        var fixture = CreateFixture(referenceDate: new DateOnly(2026, 6, 9));
        fixture.WeeklyClosingRead.Carryover = new WeeklyDoughCarryoverResponse
        {
            HasClosingCarryover = true,
            SourceWeekStartDate = new DateOnly(2026, 6, 2),
            SourceWeekEndDate = new DateOnly(2026, 6, 7),
            CarryoverReadyBalls = 300,
            CarryoverAttentionBalls = 0,
            CarryoverAvailableBalls = 300,
            MixedButNotBalledLoads = 1,
            PreviousWeekProducedBalls = 900,
            PreviousWeekUsedBalls = 723,
            PreviousWeekLostBalls = 18
        };

        var result = await fixture.Service.GetWeekAsync(
            new DateOnly(2026, 6, 9),
            historicalWeeksToUse: 8);

        Assert.True(result.HasClosingCarryover);
        Assert.Equal(300, result.CarryoverAvailableBalls);
        Assert.Equal(1, result.CarryoverMixedButNotBalledLoads);
        Assert.Equal(168, result.CarryoverMixedButNotBalledPotentialBalls);
        Assert.Equal(300, result.ReadyNowBalls);
        Assert.Equal(168, result.MixedButNotBalledBalls);
        Assert.Equal(723, result.PreviousWeekFinishedBalls);
        Assert.Equal(300, result.StillMissingThisWeekBalls);
    }

    [Fact]
    public async Task In_Process_Dough_StaysInFutureDough_AndDoesNotCountAsReadyNow()
    {
        var fixture = CreateFixture(referenceDate: new DateOnly(2026, 6, 9));
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(
            snapshotDate: new DateOnly(2026, 6, 9),
            availableBalls: 24));

        fixture.Batches.Batches.Add(new DoughBatch(
            Guid.NewGuid(),
            batchDate: new DateOnly(2026, 6, 8),
            totalCases: DoughBatch.StandardLoadCases));

        fixture.Batches.Batches.Add(new DoughBatch(
            Guid.NewGuid(),
            batchDate: new DateOnly(2026, 6, 7),
            totalCases: DoughBatch.StandardLoadCases));

        var result = await fixture.Service.GetWeekAsync(
            new DateOnly(2026, 6, 9),
            historicalWeeksToUse: 8);

        Assert.Equal(24, result.ReadyNowBalls);
        Assert.Equal(0, result.StillFermentingBalls);
        Assert.Equal(336, result.MixedButNotBalledBalls);
        Assert.Equal(576, result.StillMissingThisWeekBalls);
    }

    [Fact]
    public async Task Voided_Orphan_Batch_DoesNotCountAsMixedButNotBalled()
    {
        var referenceDate = new DateOnly(2026, 6, 9);
        var fixture = CreateFixture(referenceDate);

        var orphanBatch = new DoughBatch(
            Guid.NewGuid(),
            batchDate: referenceDate,
            totalCases: DoughBatch.StandardLoadCases);
        orphanBatch.Void("orphan batch correction");
        fixture.Batches.Batches.Add(orphanBatch);

        var result = await fixture.Service.GetWeekAsync(referenceDate, historicalWeeksToUse: 8);

        Assert.Equal(0, result.MixedButNotBalledLoads);
        Assert.Equal(0, result.MixedButNotBalledBalls);
        Assert.Equal(0, result.FutureBalls);
    }

    [Fact]
    public async Task Tuesday_Scenario_Shows_One_Ready_Load_And_Two_Mixed_Loads_Separately()
    {
        var fixture = CreateFixture(referenceDate: new DateOnly(2026, 6, 9));
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(
            snapshotDate: new DateOnly(2026, 6, 9),
            availableBalls: 168));

        fixture.Batches.Batches.Add(new DoughBatch(
            Guid.NewGuid(),
            batchDate: new DateOnly(2026, 6, 7),
            totalCases: DoughBatch.StandardLoadCases));

        fixture.Batches.Batches.Add(new DoughBatch(
            Guid.NewGuid(),
            batchDate: new DateOnly(2026, 6, 7),
            totalCases: DoughBatch.StandardLoadCases));

        var result = await fixture.Service.GetWeekAsync(
            new DateOnly(2026, 6, 9),
            historicalWeeksToUse: 8);

        Assert.Equal(168, result.ReadyNowBalls);
        Assert.Equal(336, result.MixedButNotBalledBalls);
    }

    [Fact]
    public async Task TuesdayScenario_432Ready_OneMixedLoad_KeepsFutureDoughSeparateFromStillMissing()
    {
        var tuesday = new DateOnly(2026, 6, 9);
        var fixture = CreateFixture(referenceDate: tuesday);
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(tuesday, availableBalls: 432));

        fixture.Batches.Batches.Add(new DoughBatch(
            Guid.NewGuid(),
            batchDate: tuesday,
            totalCases: DoughBatch.StandardLoadCases));

        var result = await fixture.Service.GetWeekAsync(tuesday, historicalWeeksToUse: 8);

        Assert.Equal(432, result.ReadyNowBalls);
        Assert.Equal(168, result.MixedButNotBalledBalls);
        Assert.Equal(1, result.MixedButNotBalledLoads);
        Assert.Equal(0, result.StillFermentingBalls);
        Assert.Equal(168, result.StillMissingThisWeekBalls);
    }

    [Fact]
    public async Task BallDoughCompletion_IncreasesReady_AndRemovesMixedLoad()
    {
        var tuesday = new DateOnly(2026, 6, 9);
        var fixture = CreateFixture(referenceDate: tuesday);
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(tuesday, availableBalls: 600));

        var batch = new DoughBatch(
            Guid.NewGuid(),
            batchDate: tuesday,
            totalCases: DoughBatch.StandardLoadCases);
        batch.MarkAsBalled(new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc));
        fixture.Batches.Batches.Add(batch);

        fixture.Tasks.Tasks.Add(CreateCompletedTask(
            taskDate: tuesday,
            completedAtUtc: new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc),
            quantityCompleted: 168));

        var result = await fixture.Service.GetWeekAsync(tuesday, historicalWeeksToUse: 8);

        Assert.Equal(600, result.ReadyNowBalls);
        Assert.Equal(0, result.MixedButNotBalledBalls);
        Assert.Equal(168, result.ProducedThisWeekBalls);
        Assert.Equal(0, result.StillMissingThisWeekBalls);
    }

    [Fact]
    public async Task TwoFullLoadsPlusEightExtraCases_Equals432ReadyBalls()
    {
        var tuesday = new DateOnly(2026, 6, 9);
        var fixture = CreateFixture(referenceDate: tuesday);
        var readyBalls = (2 * DoughRules.StandardBatchBalls) + (8 * DoughRules.BallsPerCase);
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(tuesday, readyBalls));

        var result = await fixture.Service.GetWeekAsync(tuesday, historicalWeeksToUse: 8);

        Assert.Equal(432, result.ReadyNowBalls);
    }

    [Fact]
    public async Task CarryoverAnchoredReady_FixesLowSnapshotAfterBallDoughCompletion()
    {
        var tuesday = new DateOnly(2026, 6, 9);
        var fixture = CreateFixture(referenceDate: tuesday);
        fixture.WeeklyClosingRead.Carryover = new WeeklyDoughCarryoverResponse
        {
            HasClosingCarryover = true,
            CarryoverAvailableBalls = 432,
            MixedButNotBalledLoads = 0
        };
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(tuesday, availableBalls: 192));

        var batch = new DoughBatch(
            Guid.NewGuid(),
            batchDate: tuesday,
            totalCases: DoughBatch.StandardLoadCases);
        batch.MarkAsBalled(new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc));
        fixture.Batches.Batches.Add(batch);

        fixture.Tasks.Tasks.Add(CreateCompletedTask(
            taskDate: tuesday,
            completedAtUtc: new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc),
            quantityCompleted: 168));

        fixture.DailyClosings.Items.Add(DailyDoughClosing.Create(
            tuesday,
            tuesday,
            forecastNeededBalls: 175,
            actualUsedBalls: 45,
            closedByUserId: "manager-user",
            closedAtUtc: DateTime.UtcNow));

        var result = await fixture.Service.GetWeekAsync(tuesday, historicalWeeksToUse: 8);

        Assert.Equal(555, result.ReadyNowBalls);
        Assert.Equal(0, result.MixedButNotBalledBalls);
        Assert.Equal(0, result.StillFermentingBalls);
        Assert.Equal(0, result.StillMissingThisWeekBalls);
    }

    [Fact]
    public async Task OpenUsageTracesReduceReadyNowButDoNotReduceProducedThisWeek()
    {
        var tuesday = new DateOnly(2026, 6, 9);
        var fixture = CreateFixture(referenceDate: tuesday);
        fixture.WeeklyClosingRead.Carryover = new WeeklyDoughCarryoverResponse
        {
            HasClosingCarryover = true,
            CarryoverAvailableBalls = 432,
            CarryoverReadyBalls = 432
        };
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(tuesday, availableBalls: 432));

        var batch = new DoughBatch(
            Guid.NewGuid(),
            batchDate: tuesday,
            totalCases: DoughBatch.StandardLoadCases);
        batch.MarkAsBalled(new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc));
        fixture.Batches.Batches.Add(batch);

        fixture.Tasks.Tasks.Add(CreateCompletedTask(
            taskDate: tuesday,
            completedAtUtc: new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc),
            quantityCompleted: 168));

        fixture.UsageTraces.Items.Add(DoughUsageTrace.Create(
            usageDate: tuesday,
            sourceDoughBatchQualityRecordId: Guid.NewGuid(),
            sourceDate: tuesday.AddDays(-1),
            sourceType: DoughQualityStatus.Good,
            destination: DoughUsageDestination.Restaurant,
            trayCount: 1.5m,
            createdByUserId: "manager-user"));

        var result = await fixture.Service.GetWeekAsync(tuesday, historicalWeeksToUse: 8);

        Assert.Equal(582, result.ReadyNowBalls);
        Assert.Equal(168, result.ProducedThisWeekBalls);
        Assert.Equal(0, result.FutureBalls);
    }

    [Fact]
    public async Task OnePendingLoad_IsCountedOnce_EvenWithMakeTask_BallTask_AndUnballedBatch()
    {
        var referenceDate = new DateOnly(2026, 6, 17);
        var fixture = CreateFixture(referenceDate);
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(referenceDate, availableBalls: 720));

        var pendingBatch = new DoughBatch(
            Guid.NewGuid(),
            batchDate: referenceDate,
            totalCases: DoughBatch.StandardLoadCases);
        fixture.Batches.Batches.Add(pendingBatch);

        fixture.Tasks.Tasks.Add(CreateCompletedLoadTask(
            taskDate: referenceDate,
            completedAtUtc: new DateTime(2026, 6, 17, 15, 0, 0, DateTimeKind.Utc),
            quantityCompleted: 1));
        fixture.Tasks.Tasks.Add(CreatePendingBallTask(
            taskDate: referenceDate.AddDays(1),
            sourceDoughBatchId: pendingBatch.Id,
            quantityRecommended: DoughRules.StandardBatchBalls));

        var result = await fixture.Service.GetWeekAsync(referenceDate, historicalWeeksToUse: 8);

        Assert.Equal(720, result.ReadyNowBalls);
        Assert.Equal(168, result.MixedButNotBalledBalls);
        Assert.Equal(1, result.MixedButNotBalledLoads);
        Assert.Equal(168, result.FutureBalls);
    }

    [Fact]
    public async Task ReadyNow720_Future168_StillMissing223_ForFourLoadsAndFourReballedCases()
    {
        var referenceDate = new DateOnly(2026, 6, 17);
        var readyFromCompletedLoads = 4 * DoughRules.StandardBatchBalls;
        var readyFromReballedCases = 4 * DoughRules.BallsPerCase;
        var fixture = CreateFixture(
            referenceDate,
            new SingleDayRequiredBallsCalculationService(referenceDate, requiredBalls: 943));
        fixture.InventorySnapshots.Snapshots.Add(CreateSnapshot(
            referenceDate,
            availableBalls: readyFromCompletedLoads + readyFromReballedCases));

        fixture.QualityRecords.Records.Add(CreateQualityRecord(
            sourceDate: referenceDate.AddDays(-3),
            createdOrBalledAtUtc: new DateTime(2026, 6, 14, 10, 0, 0, DateTimeKind.Utc),
            quantityBalls: 168,
            status: DoughQualityStatus.Good));
        fixture.QualityRecords.Records.Add(CreateQualityRecord(
            sourceDate: referenceDate.AddDays(-2),
            createdOrBalledAtUtc: new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            quantityBalls: 168,
            status: DoughQualityStatus.Good));
        fixture.QualityRecords.Records.Add(CreateQualityRecord(
            sourceDate: referenceDate.AddDays(-1),
            createdOrBalledAtUtc: new DateTime(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc),
            quantityBalls: 168,
            status: DoughQualityStatus.Good));
        fixture.QualityRecords.Records.Add(CreateQualityRecord(
            sourceDate: referenceDate,
            createdOrBalledAtUtc: new DateTime(2026, 6, 17, 10, 0, 0, DateTimeKind.Utc),
            quantityBalls: 168,
            status: DoughQualityStatus.Good));
        fixture.QualityRecords.Records.Add(CreateQualityRecord(
            sourceDate: referenceDate,
            createdOrBalledAtUtc: new DateTime(2026, 6, 17, 11, 0, 0, DateTimeKind.Utc),
            quantityBalls: 48,
            status: DoughQualityStatus.Reballed));

        fixture.Batches.Batches.Add(new DoughBatch(
            Guid.NewGuid(),
            batchDate: referenceDate,
            totalCases: DoughBatch.StandardLoadCases));

        var result = await fixture.Service.GetWeekAsync(referenceDate, historicalWeeksToUse: 8);

        Assert.Equal(672, readyFromCompletedLoads);
        Assert.Equal(48, readyFromReballedCases);
        Assert.Equal(720, readyFromCompletedLoads + readyFromReballedCases);
        Assert.Equal(720, result.ReadyNowBalls);
        Assert.Equal(168, result.MixedButNotBalledBalls);
        Assert.Equal(1, result.MixedButNotBalledLoads);
        Assert.Equal(168, result.FutureBalls);
        Assert.Equal(223, result.StillMissingThisWeekBalls);
    }

    private static TestFixture CreateFixture(DateOnly referenceDate, IDoughPrepCalculationService? calculationService = null)
    {
        calculationService ??= new FixedWeeklyCalculationService(referenceDate);
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

        return new TestFixture(
            batches,
            inventorySnapshots,
            tasks,
            weeklyClosingRead,
            dailyClosings,
            qualityRecords,
            lossRecords,
            usageTraces,
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

    private static PrepTask CreateCompletedTask(
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

    private static PrepTask CreateCompletedLoadTask(
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
            taskType: PrepTaskType.MakeDoughLoad,
            quantityUnit: DoughQuantityUnit.FullLoads);

        task.Complete("user-1", quantityCompleted, completedAtUtc: completedAtUtc);
        return task;
    }

    private static PrepTask CreatePendingBallTask(
        DateOnly taskDate,
        Guid sourceDoughBatchId,
        int quantityRecommended)
    {
        return PrepTask.Create(
            taskDate,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ApplicationRole.PizzaMaker,
            quantityRecommended: quantityRecommended,
            taskType: PrepTaskType.BallDough,
            quantityUnit: DoughQuantityUnit.Balls,
            sourceDoughBatchId: sourceDoughBatchId);
    }

    private static DoughBatchQualityRecord CreateQualityRecord(
        DateOnly sourceDate,
        DateTime createdOrBalledAtUtc,
        int quantityBalls,
        DoughQualityStatus status)
    {
        return DoughBatchQualityRecord.Create(
            sourceDate,
            createdOrBalledAtUtc,
            quantityBalls,
            createdByUserId: "manager-user",
            initialStatus: status);
    }

    private sealed record TestFixture(
        InMemoryDoughBatchReadRepository Batches,
        InMemoryDoughInventoryReadRepository InventorySnapshots,
        InMemoryPrepTaskRepository Tasks,
        StubWeeklyDoughClosingReadService WeeklyClosingRead,
        InMemoryDailyDoughClosingRepository DailyClosings,
        InMemoryDoughBatchQualityRepository QualityRecords,
        InMemoryDoughLossRecordRepository LossRecords,
        InMemoryDoughUsageTraceRepository UsageTraces,
        PrepWeeklyDoughCalendarService Service);

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

    private sealed class SingleDayRequiredBallsCalculationService : IDoughPrepCalculationService
    {
        private readonly DateOnly _targetDate;
        private readonly int _requiredBalls;

        public SingleDayRequiredBallsCalculationService(DateOnly targetDate, int requiredBalls)
        {
            _targetDate = targetDate;
            _requiredBalls = requiredBalls;
        }

        public Task<DoughPrepCalculationResult> CalculateAsync(
            CalculateDoughPrepRequest request,
            CancellationToken cancellationToken = default)
        {
            var requiredBalls = request.TargetDate == _targetDate
                ? _requiredBalls
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
                Reason = "single-day-required-balls-test"
            });
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
                Batches
                    .Where(batch => batch.BatchDate <= productionDate && !batch.IsVoided)
                    .ToArray());
        }

        public Task<IReadOnlyCollection<DoughBatch>> SearchForCorrectionAsync(
            DateOnly? batchDateFrom,
            DateOnly? batchDateTo,
            bool includeVoided,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<DoughBatch> query = Batches;

            if (batchDateFrom.HasValue)
            {
                query = query.Where(batch => batch.BatchDate >= batchDateFrom.Value);
            }

            if (batchDateTo.HasValue)
            {
                query = query.Where(batch => batch.BatchDate <= batchDateTo.Value);
            }

            if (!includeVoided)
            {
                query = query.Where(batch => !batch.IsVoided);
            }

            return Task.FromResult<IReadOnlyCollection<DoughBatch>>(query.ToArray());
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
                Tasks
                    .Where(task => task.TaskDate >= startDate && task.TaskDate <= endDate)
                    .ToArray());
        }

        public Task<IReadOnlyList<PrepTask>> SearchDoughTasksAsync(DateOnly? taskDate, PrepTaskStatus? status, ApplicationRole? assignedRole, Guid? prepItemId, bool includeCancelled = false, CancellationToken cancellationToken = default)
        {
            IEnumerable<PrepTask> query = Tasks;

            if (taskDate.HasValue)
            {
                query = query.Where(task => task.TaskDate == taskDate.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(task => task.Status == status.Value);
            }

            if (assignedRole.HasValue)
            {
                query = query.Where(task => task.AssignedRole == assignedRole.Value);
            }

            if (prepItemId.HasValue && prepItemId.Value != Guid.Empty)
            {
                query = query.Where(task => task.PrepItemId == prepItemId.Value);
            }

            return Task.FromResult<IReadOnlyList<PrepTask>>(query.ToArray());
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
