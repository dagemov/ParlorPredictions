using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Application.Services.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class DoughAvailabilityProjectionServiceTests
{
    [Fact]
    public async Task PreviousCarryover300_DailyUsed100_Leaves200Available()
    {
        var fixture = CreateFixture();
        fixture.WeeklyClosingRead.Carryover = CreateCarryover(300);
        fixture.DailyClosings.Items.Add(CreateDailyClosing(
            closingDate: new DateOnly(2026, 6, 9),
            weekStartDate: new DateOnly(2026, 6, 9),
            actualUsedBalls: 100));

        var projection = await fixture.Service.GetWeeklyAvailabilityAsync(new DateOnly(2026, 6, 9));

        Assert.Equal(200, projection.AvailableBalls);
        Assert.Equal(200, projection.RegularReadyBalls);
    }

    [Fact]
    public async Task DiscardedLossReducesAvailableCarryover()
    {
        var fixture = CreateFixture();
        fixture.WeeklyClosingRead.Carryover = CreateCarryover(300);
        fixture.Losses.Records.Add(DoughLossRecord.Create(
            Guid.NewGuid(),
            quantityLostBalls: 40,
            lossReason: DoughLossReason.ManagerDecision,
            lossDate: new DateOnly(2026, 6, 9),
            createdByUserId: "manager-user"));

        var projection = await fixture.Service.GetWeeklyAvailabilityAsync(new DateOnly(2026, 6, 9));

        Assert.Equal(260, projection.AvailableBalls);
        Assert.Equal(260, projection.RegularReadyBalls);
    }

    [Fact]
    public async Task AttentionCountsAsAvailableButStaysSeparate()
    {
        var fixture = CreateFixture();
        fixture.WeeklyClosingRead.Carryover = CreateCarryover(300);
        fixture.QualityRecords.Records.Add(DoughBatchQualityRecord.Create(
            sourceDate: new DateOnly(2026, 6, 8),
            createdOrBalledAt: new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc),
            quantityBalls: 80,
            createdByUserId: "manager-user",
            initialStatus: DoughQualityStatus.Attention,
            statusReason: "review"));

        var projection = await fixture.Service.GetWeeklyAvailabilityAsync(new DateOnly(2026, 6, 9));

        Assert.Equal(300, projection.AvailableBalls);
        Assert.Equal(80, projection.AttentionAvailableBalls);
        Assert.Equal(220, projection.RegularReadyBalls);
    }

    [Fact]
    public async Task MustUseNextDayCountsAsAvailableAndAppearsAsUseFirst()
    {
        var fixture = CreateFixture();
        fixture.WeeklyClosingRead.Carryover = CreateCarryover(300);
        fixture.QualityRecords.Records.Add(DoughBatchQualityRecord.Create(
            sourceDate: new DateOnly(2026, 6, 8),
            createdOrBalledAt: new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc),
            quantityBalls: 60,
            createdByUserId: "manager-user",
            initialStatus: DoughQualityStatus.MustUseNextDay,
            mustUseByDate: new DateOnly(2026, 6, 10)));

        var projection = await fixture.Service.GetWeeklyAvailabilityAsync(new DateOnly(2026, 6, 9));

        Assert.Equal(300, projection.AvailableBalls);
        Assert.Equal(60, projection.MustUseNextDayBalls);
        Assert.Equal(240, projection.RegularReadyBalls);
    }

    [Fact]
    public async Task OpenUsageTracesReduceAvailableBallsBeforeDailyClosing()
    {
        var fixture = CreateFixture();
        fixture.WeeklyClosingRead.Carryover = CreateCarryover(300);
        fixture.UsageTraces.Items.Add(DoughUsageTrace.Create(
            usageDate: new DateOnly(2026, 6, 10),
            sourceDoughBatchQualityRecordId: Guid.NewGuid(),
            sourceDate: new DateOnly(2026, 6, 9),
            sourceType: DoughQualityStatus.Good,
            destination: DoughUsageDestination.Restaurant,
            trayCount: 2m,
            createdByUserId: "manager-user"));

        var projection = await fixture.Service.GetWeeklyAvailabilityAsync(new DateOnly(2026, 6, 10));

        Assert.Equal(276, projection.AvailableBalls);
    }

    [Fact]
    public async Task ClosedDayUsageTracesDoNotDoubleReduceAvailability()
    {
        var fixture = CreateFixture();
        fixture.WeeklyClosingRead.Carryover = CreateCarryover(300);
        fixture.DailyClosings.Items.Add(CreateDailyClosing(
            closingDate: new DateOnly(2026, 6, 9),
            weekStartDate: new DateOnly(2026, 6, 9),
            actualUsedBalls: 100));
        fixture.UsageTraces.Items.Add(DoughUsageTrace.Create(
            usageDate: new DateOnly(2026, 6, 9),
            sourceDoughBatchQualityRecordId: Guid.NewGuid(),
            sourceDate: new DateOnly(2026, 6, 8),
            sourceType: DoughQualityStatus.Good,
            destination: DoughUsageDestination.Restaurant,
            trayCount: 2m,
            createdByUserId: "manager-user"));

        var projection = await fixture.Service.GetWeeklyAvailabilityAsync(new DateOnly(2026, 6, 10));

        Assert.Equal(200, projection.AvailableBalls);
    }

    private static WeeklyDoughCarryoverResponse CreateCarryover(int carryoverAvailableBalls)
    {
        return new WeeklyDoughCarryoverResponse
        {
            HasClosingCarryover = true,
            SourceWeekStartDate = new DateOnly(2026, 6, 2),
            SourceWeekEndDate = new DateOnly(2026, 6, 7),
            CarryoverReadyBalls = carryoverAvailableBalls,
            CarryoverAvailableBalls = carryoverAvailableBalls
        };
    }

    private static DailyDoughClosing CreateDailyClosing(
        DateOnly closingDate,
        DateOnly weekStartDate,
        int actualUsedBalls)
    {
        return DailyDoughClosing.Create(
            closingDate,
            weekStartDate,
            forecastNeededBalls: actualUsedBalls + 20,
            actualUsedBalls: actualUsedBalls,
            closedByUserId: "manager-user",
            closedAtUtc: DateTime.UtcNow);
    }

    private static TestFixture CreateFixture()
    {
        var dailyClosings = new InMemoryDailyDoughClosingRepository();
        var qualityRecords = new InMemoryDoughBatchQualityRepository();
        var inventorySnapshots = new InMemoryDoughInventoryReadRepository();
        var losses = new InMemoryDoughLossRecordRepository();
        var tasks = new InMemoryPrepTaskRepository();
        var usageTraces = new InMemoryDoughUsageTraceRepository();
        var weeklyClosingRead = new StubWeeklyDoughClosingReadService();
        var sourceProjectionService = new DoughSourceProjectionService(qualityRecords, usageTraces);

        return new TestFixture(
            dailyClosings,
            qualityRecords,
            inventorySnapshots,
            losses,
            tasks,
            usageTraces,
            weeklyClosingRead,
            new DoughAvailabilityProjectionService(
                dailyClosings,
                sourceProjectionService,
                usageTraces,
                inventorySnapshots,
                losses,
                tasks,
                weeklyClosingRead));
    }

    private sealed record TestFixture(
        InMemoryDailyDoughClosingRepository DailyClosings,
        InMemoryDoughBatchQualityRepository QualityRecords,
        InMemoryDoughInventoryReadRepository InventorySnapshots,
        InMemoryDoughLossRecordRepository Losses,
        InMemoryPrepTaskRepository Tasks,
        InMemoryDoughUsageTraceRepository UsageTraces,
        StubWeeklyDoughClosingReadService WeeklyClosingRead,
        DoughAvailabilityProjectionService Service);

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

        public Task<IReadOnlyList<DailyDoughClosing>> ListByWeekStartDateAsync(DateOnly weekStartDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DailyDoughClosing>>(
                Items.Where(item => item.WeekStartDate == weekStartDate).ToArray());
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
            return Task.FromResult<IReadOnlyList<DoughBatchQualityRecord>>(Records.ToArray());
        }
    }

    private sealed class InMemoryDoughInventoryReadRepository : IDoughInventoryReadRepository
    {
        public Task<DoughInventorySnapshot?> GetLatestSnapshotOnOrBeforeAsync(DateOnly targetDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DoughInventorySnapshot?>(null);
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
            return Task.FromResult<PrepTask?>(Tasks.FirstOrDefault(task => task.Id == id));
        }

        public Task<PrepTask?> GetByDoughPrepRecommendationIdAsync(Guid doughPrepRecommendationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PrepTask?>(Tasks.FirstOrDefault(task => task.DoughPrepRecommendationId == doughPrepRecommendationId));
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
}
