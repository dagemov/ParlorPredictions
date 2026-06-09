using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Application.Services.Prep;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
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
        Assert.Equal(132, result.StillMissingThisWeekBalls);
    }

    [Fact]
    public async Task In_Process_Dough_Reduces_Weekly_Missing_But_Does_Not_Count_As_Ready_Now()
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
        Assert.Equal(168, result.StillFermentingBalls);
        Assert.Equal(168, result.MixedButNotBalledBalls);
        Assert.Equal(240, result.StillMissingThisWeekBalls);
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

    private static TestFixture CreateFixture(DateOnly referenceDate)
    {
        var calculationService = new FixedWeeklyCalculationService(referenceDate);
        var batches = new InMemoryDoughBatchReadRepository();
        var inventorySnapshots = new InMemoryDoughInventoryReadRepository();
        var tasks = new InMemoryPrepTaskRepository();
        var weeklyClosingRead = new StubWeeklyDoughClosingReadService();

        return new TestFixture(
            batches,
            inventorySnapshots,
            tasks,
            weeklyClosingRead,
            new PrepWeeklyDoughCalendarService(
                calculationService,
                batches,
                inventorySnapshots,
                tasks,
                weeklyClosingRead));
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

    private sealed record TestFixture(
        InMemoryDoughBatchReadRepository Batches,
        InMemoryDoughInventoryReadRepository InventorySnapshots,
        InMemoryPrepTaskRepository Tasks,
        StubWeeklyDoughClosingReadService WeeklyClosingRead,
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

    private sealed class InMemoryDoughBatchReadRepository : IDoughBatchReadRepository
    {
        public List<DoughBatch> Batches { get; } = [];

        public Task<IReadOnlyCollection<DoughBatch>> GetProducedOnOrBeforeAsync(
            DateOnly productionDate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<DoughBatch>>(
                Batches
                    .Where(batch => batch.BatchDate <= productionDate)
                    .ToArray());
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

        public Task<IReadOnlyList<PrepTask>> SearchDoughTasksAsync(DateOnly? taskDate, PrepTaskStatus? status, ApplicationRole? assignedRole, Guid? prepItemId, CancellationToken cancellationToken = default)
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
}
