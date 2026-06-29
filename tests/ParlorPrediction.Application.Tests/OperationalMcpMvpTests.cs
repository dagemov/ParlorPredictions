using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Application.Services.AIOrchestration;
using ParlorPrediction.Application.Services.OperationalDrafts;
using ParlorPrediction.Application.Services.OperationalSimulation;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mcp.Contracts;
using ParlorPrediction.Mcp.Security;
using ParlorPrediction.Mcp.Tools;
using ParlorPrediction.Persistence;
using ParlorPrediction.Persistence.Repositories;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class OperationalMcpMvpTests
{
    [Fact]
    public async Task OperationalIntentClassifier_MapsThreeLinesAndNoPendingLoadToWeeklyClosing()
    {
        var classifier = new OperationalIntentClassifier();

        var intent = await classifier.ClassifyAsync(
            "Esta semana sobraron 3 lineas y no quedo carga pendiente. El domingo se hizo una carga y el lunes se boleo.",
            new DateOnly(2026, 6, 21),
            new DateOnly(2026, 6, 15));

        var weeklyClosingIntent = Assert.IsType<WeeklyClosingIntent>(intent);
        Assert.Equal(new DateOnly(2026, 6, 15), weeklyClosingIntent.WeekStartDate);
        Assert.Equal(3, weeklyClosingIntent.LinesLeftover);
        Assert.Equal(504, weeklyClosingIntent.LeftoverReadyBalls);
        Assert.Equal(0, weeklyClosingIntent.LeftoverMixedLoads);
        Assert.True(weeklyClosingIntent.SundayLoadBalledMonday);
    }

    [Fact]
    public async Task OperationalSimulationService_Produces504ReadyAndZeroMixedForSundayLoadBalledMonday()
    {
        using var harness = CreateHarness();

        var simulation = await harness.SimulationService.SimulateAsync(CreatePrimaryNarrativeRequest());

        Assert.Equal(OperationalIntentKind.WeeklyClosing, simulation.Intent.Kind);
        Assert.NotNull(simulation.WeeklyCorrectionProposal);
        Assert.Equal(new DateOnly(2026, 6, 15), simulation.WeeklyCorrectionProposal!.WeekStartDate);
        Assert.Equal(504, simulation.WeeklyCorrectionProposal.LeftoverReadyBalls);
        Assert.Equal(0, simulation.WeeklyCorrectionProposal.LeftoverMixedLoads);
        Assert.NotNull(simulation.DoughTaskDraftProposal);
        Assert.Equal("BallDough", simulation.DoughTaskDraftProposal!.TaskType);
        Assert.Contains("existing-weekly-closing", simulation.ValidationWarningsJson, StringComparison.Ordinal);
        Assert.Contains("504", simulation.DiffJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WeeklyTools_DraftWeeklyCorrection_CreatesDraftAndAuditEntry()
    {
        using var harness = CreateHarness();
        var tools = new WeeklyTools(
            harness.DraftService,
            harness.WeeklyClosingReadService,
            new McpToolAllowlist());

        var result = await tools.DraftWeeklyCorrectionAsync(new DraftWeeklyCorrectionToolRequest
        {
            SourceText = CreatePrimaryNarrativeRequest().SourceText,
            ReferenceDate = new DateOnly(2026, 6, 21),
            TargetWeekStartDate = new DateOnly(2026, 6, 15),
            HistoricalWeeksToUse = 8,
            ActorUserId = "admin-user"
        });

        Assert.Equal("WeeklyCorrection", result.Draft.DraftType);
        Assert.Equal("admin-user", result.Draft.CreatedBy);
        Assert.Equal(OperationalDraftStatus.Pending, result.Draft.Status);
        Assert.Equal(result.Draft.Id, result.AuditEntry.DraftId);
        Assert.Contains("504", result.DiffJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OperationalDraft_PersistenceSurvivesRestart()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString("N");

        Guid draftId;
        Guid correlationId;

        await using (var firstHarness = CreateHarness(databaseRoot, databaseName))
        {
            var created = await firstHarness.DraftService.CreateWeeklyCorrectionDraftAsync(CreatePrimaryNarrativeRequest());
            draftId = created.Draft.Id;
            correlationId = created.Draft.CorrelationId;
        }

        await using var secondHarness = CreateHarness(databaseRoot, databaseName);
        var persistedDraft = await secondHarness.DraftRepository.GetByIdAsync(draftId);
        var persistedAuditEntries = await secondHarness.AuditRepository.ListByCorrelationIdAsync(correlationId);

        Assert.NotNull(persistedDraft);
        Assert.Equal(OperationalDraftStatus.Pending, persistedDraft!.Status);
        Assert.Equal("admin-user", persistedDraft.CreatedBy);
        Assert.Equal(2, persistedAuditEntries.Count);
    }

    [Fact]
    public async Task OperationalSimulationService_DuplicateLoadDetectionBlocksInvalidSimulation()
    {
        var scenario = TestScenario.CreateDefault();
        scenario.Closings = [];
        scenario.WeeklyGoal = new WeeklyDoughCalendarResponse
        {
            WeekStartDate = new DateOnly(2026, 6, 16),
            WeekEndDate = new DateOnly(2026, 6, 21),
            WeekTotalNeededBalls = 943,
            ReadyNowBalls = 504,
            MixedButNotBalledBalls = 0,
            MixedButNotBalledLoads = 0,
            StillFermentingBalls = 0,
            FutureBalls = 0,
            StillMissingThisWeekBalls = 439,
            ProducedThisWeekBalls = 1008,
            ActualUsedBallsThisWeek = 1010,
            CarryoverAvailableBalls = 296,
            CarryoverMixedButNotBalledLoads = 0
        };
        scenario.Batches = [];

        using var harness = CreateHarness(scenario: scenario);

        var simulation = await harness.SimulationService.SimulateAsync(new OperationalNarrativeRequest
        {
            SourceText = "Esta semana quedo 1 carga pendiente.",
            ReferenceDate = new DateOnly(2026, 6, 21),
            TargetWeekStartDate = new DateOnly(2026, 6, 15),
            HistoricalWeeksToUse = 8,
            ActorUserId = "admin-user"
        });

        Assert.Contains(simulation.ValidationWarnings, warning => warning.Code == "duplicate-load-prevention" && warning.BlocksDraft);
        Assert.Contains(simulation.ValidationWarnings, warning => warning.Code == "mixed-load-physical-mismatch" && warning.BlocksDraft);
    }

    [Fact]
    public async Task OperationalDraft_ApprovalChangesStatusCorrectly()
    {
        await using var harness = CreateHarness();
        var created = await harness.DraftService.CreateWeeklyCorrectionDraftAsync(CreatePrimaryNarrativeRequest());
        var ready = await harness.DraftService.MarkAsReadyForApprovalAsync(created.Draft.Id);
        Assert.Equal(OperationalDraftStatus.ReadyForApproval, ready.Draft.Status);

        var approved = await harness.DraftService.ApproveDraftAsync(created.Draft.Id, "admin-user");
        var persistedDraft = await harness.DraftRepository.GetByIdAsync(created.Draft.Id);

        Assert.Equal(OperationalDraftStatus.Approved, approved.Draft.Status);
        Assert.Equal("admin-user", approved.Draft.ReviewedByUserId);
        Assert.Equal(approved.ApprovedEntityId, approved.Draft.ApprovedEntityId);
        Assert.NotNull(persistedDraft);
        Assert.Equal(OperationalDraftStatus.Approved, persistedDraft!.Status);
        Assert.Single(harness.WeeklyClosingManagementService.CorrectRequests);
    }

    [Fact]
    public async Task OperationalSimulationService_CreatesAuditEntryForEachSimulationCall()
    {
        await using var harness = CreateHarness();
        var correlationId = Guid.NewGuid();
        var request = CreatePrimaryNarrativeRequest(correlationId);

        await harness.SimulationService.SimulateAsync(request);
        await harness.SimulationService.SimulateAsync(request);

        var auditEntries = await harness.AuditRepository.ListByCorrelationIdAsync(correlationId);

        Assert.Equal(2, auditEntries.Count);
        Assert.All(auditEntries, entry => Assert.Equal("SimulateOperationalNarrative", entry.ActionType));
    }

    private static OperationalNarrativeRequest CreatePrimaryNarrativeRequest(Guid? correlationId = null)
    {
        return new OperationalNarrativeRequest
        {
            CorrelationId = correlationId,
            SourceText = "Esta semana sobraron 3 lineas y no quedo carga pendiente. El domingo se hizo una carga y el lunes se boleo.",
            ReferenceDate = new DateOnly(2026, 6, 21),
            TargetWeekStartDate = new DateOnly(2026, 6, 15),
            HistoricalWeeksToUse = 8,
            ActorUserId = "admin-user"
        };
    }

    private static OperationalTestHarness CreateHarness(
        InMemoryDatabaseRoot? databaseRoot = null,
        string? databaseName = null,
        TestScenario? scenario = null)
    {
        scenario ??= TestScenario.CreateDefault();
        databaseRoot ??= new InMemoryDatabaseRoot();
        databaseName ??= Guid.NewGuid().ToString("N");

        var options = new DbContextOptionsBuilder<ParlorPredictionDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;

        var dbContext = new ParlorPredictionDbContext(options);
        dbContext.Database.EnsureCreated();

        var unitOfWork = new DbContextBackedUnitOfWork(dbContext);
        var draftRepository = new OperationalDraftRepository(dbContext);
        var auditRepository = new OperationalAuditEntryRepository(dbContext);
        var weeklyClosingReadService = new StubWeeklyDoughClosingReadService(scenario);
        var weeklyClosingManagementService = new RecordingWeeklyDoughClosingManagementService();
        var prepTaskService = new RecordingPrepTaskService();
        var simulationService = new OperationalSimulationService(
            new StubDoughAvailabilityProjectionService(scenario),
            new StubDoughBatchReadRepository(scenario.Batches),
            new StubDoughInventoryImpactReadService(scenario),
            auditRepository,
            draftRepository,
            new OperationalIntentClassifier(),
            new StubPrepItemReadRepository(scenario.DoughItem),
            new StubPrepTaskRepository(scenario.Tasks),
            new StubPrepWeeklyDoughCalendarService(scenario),
            unitOfWork,
            weeklyClosingReadService);
        var draftService = new OperationalDraftService(
            auditRepository,
            draftRepository,
            simulationService,
            prepTaskService,
            unitOfWork,
            weeklyClosingManagementService);

        return new OperationalTestHarness(
            dbContext,
            draftRepository,
            auditRepository,
            simulationService,
            draftService,
            weeklyClosingReadService,
            weeklyClosingManagementService,
            prepTaskService);
    }

    private sealed class OperationalTestHarness : IAsyncDisposable, IDisposable
    {
        public OperationalTestHarness(
            ParlorPredictionDbContext dbContext,
            IOperationalDraftRepository draftRepository,
            IOperationalAuditEntryRepository auditRepository,
            OperationalSimulationService simulationService,
            OperationalDraftService draftService,
            StubWeeklyDoughClosingReadService weeklyClosingReadService,
            RecordingWeeklyDoughClosingManagementService weeklyClosingManagementService,
            RecordingPrepTaskService prepTaskService)
        {
            DbContext = dbContext;
            DraftRepository = draftRepository;
            AuditRepository = auditRepository;
            SimulationService = simulationService;
            DraftService = draftService;
            WeeklyClosingReadService = weeklyClosingReadService;
            WeeklyClosingManagementService = weeklyClosingManagementService;
            PrepTaskService = prepTaskService;
        }

        public ParlorPredictionDbContext DbContext { get; }

        public IOperationalDraftRepository DraftRepository { get; }

        public IOperationalAuditEntryRepository AuditRepository { get; }

        public OperationalSimulationService SimulationService { get; }

        public OperationalDraftService DraftService { get; }

        public StubWeeklyDoughClosingReadService WeeklyClosingReadService { get; }

        public RecordingWeeklyDoughClosingManagementService WeeklyClosingManagementService { get; }

        public RecordingPrepTaskService PrepTaskService { get; }

        public ValueTask DisposeAsync()
        {
            return DbContext.DisposeAsync();
        }

        public void Dispose()
        {
            DbContext.Dispose();
        }
    }

    private sealed class TestScenario
    {
        public IReadOnlyList<WeeklyDoughClosingResponse> Closings { get; set; } = [];

        public WeeklyDoughCarryoverResponse Carryover { get; set; } = new();

        public DoughAvailabilityProjectionResponse Availability { get; set; } = new();

        public WeeklyDoughCalendarResponse WeeklyGoal { get; set; } = new();

        public DoughInventoryImpactResponse InventoryImpact { get; set; } = new();

        public IReadOnlyList<PrepTask> Tasks { get; set; } = [];

        public IReadOnlyCollection<DoughBatch> Batches { get; set; } = [];

        public PrepItem DoughItem { get; set; } = null!;

        public static TestScenario CreateDefault()
        {
            var prepStationId = Guid.Parse("d49c2d31-6ed6-45e5-9764-b929ccfb0f11");
            var prepItemId = Guid.Parse("6f563719-c6d4-432e-a3d7-464cb0db4286");

            return new TestScenario
            {
                Closings =
                [
                    new WeeklyDoughClosingResponse
                    {
                        Id = Guid.Parse("8c6cfb36-7612-4d76-b174-74f608dca93a"),
                        WeekStartDate = new DateOnly(2026, 6, 15),
                        WeekEndDate = new DateOnly(2026, 6, 21),
                        NeededBalls = 1113,
                        ProducedBalls = 1008,
                        UsedBalls = 1010,
                        LostBalls = 0,
                        LeftoverReadyBalls = 0,
                        LeftoverAttentionBalls = 0,
                        LeftoverMixedLoads = 1,
                        CarryoverAvailableBalls = 0,
                        ClosedByUserId = "manager-user",
                        ClosedAtUtc = new DateTime(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc),
                        CorrectionNote = "Original incomplete closing before recovery."
                    }
                ],
                Carryover = new WeeklyDoughCarryoverResponse
                {
                    TargetWeekStartDate = new DateOnly(2026, 6, 15),
                    TargetWeekEndDate = new DateOnly(2026, 6, 21),
                    HasClosingCarryover = true,
                    SourceWeekStartDate = new DateOnly(2026, 6, 8),
                    SourceWeekEndDate = new DateOnly(2026, 6, 14),
                    CarryoverReadyBalls = 296,
                    CarryoverAttentionBalls = 0,
                    CarryoverAvailableBalls = 296,
                    MixedButNotBalledLoads = 1,
                    PreviousWeekProducedBalls = 672,
                    PreviousWeekUsedBalls = 923,
                    PreviousWeekLostBalls = 60,
                    ClosingNotes = "Carryover from the prior closed week."
                },
                Availability = new DoughAvailabilityProjectionResponse
                {
                    ReferenceDate = new DateOnly(2026, 6, 21),
                    WeekStartDate = new DateOnly(2026, 6, 16),
                    WeekEndDate = new DateOnly(2026, 6, 21),
                    HasClosingCarryover = true,
                    CarryoverSourceWeekStartDate = new DateOnly(2026, 6, 8),
                    CarryoverSourceWeekEndDate = new DateOnly(2026, 6, 14),
                    CarryoverReadyBalls = 296,
                    CarryoverAvailableBalls = 296,
                    CarryoverMixedButNotBalledLoads = 1,
                    PreviousWeekProducedBalls = 672,
                    PreviousWeekUsedBalls = 923,
                    PreviousWeekLostBalls = 60,
                    ProducedThisWeekBalls = 1008,
                    ActualUsedBallsThisWeek = 1010,
                    LostBallsThisWeek = 0,
                    AvailableBalls = 504,
                    RegularReadyBalls = 504,
                    AttentionAvailableBalls = 0,
                    MustUseNextDayBalls = 0
                },
                WeeklyGoal = new WeeklyDoughCalendarResponse
                {
                    WeekStartDate = new DateOnly(2026, 6, 16),
                    WeekEndDate = new DateOnly(2026, 6, 21),
                    WeekTotalNeededBalls = 943,
                    ReadyNowBalls = 504,
                    MixedButNotBalledBalls = 0,
                    MixedButNotBalledLoads = 0,
                    StillFermentingBalls = 0,
                    CarryoverMixedButNotBalledPotentialBalls = 168,
                    FutureBalls = 0,
                    PreviousWeekFinishedBalls = 923,
                    StillMissingThisWeekBalls = 439,
                    ProducedThisWeekBalls = 1008,
                    ActualUsedBallsThisWeek = 1010,
                    CarryoverAvailableBalls = 296,
                    CarryoverMixedButNotBalledLoads = 1
                },
                InventoryImpact = new DoughInventoryImpactResponse
                {
                    ReferenceDate = new DateOnly(2026, 6, 21),
                    WeekStartDate = new DateOnly(2026, 6, 16),
                    WeekEndDate = new DateOnly(2026, 6, 21),
                    WeeklyGoalBalls = 943,
                    ReadyNowBalls = 504,
                    StillMissingBalls = 439,
                    MixedButNotBalledBalls = 0,
                    FutureBalls = 0,
                    UsedTodayBalls = 95,
                    LostOrDiscardedBalls = 0,
                    RemainingTrackedBalls = 504,
                    RemainingSources = []
                },
                DoughItem = new PrepItem(prepItemId, prepStationId, "Dough", "DOUGH", "Operational dough item")
            };
        }
    }

    private sealed class DbContextBackedUnitOfWork : IUnitOfWork
    {
        private readonly ParlorPredictionDbContext _dbContext;

        public DbContextBackedUnitOfWork(ParlorPredictionDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            _dbContext.ChangeTracker.Clear();
            return Task.CompletedTask;
        }
    }

    private sealed class StubWeeklyDoughClosingReadService : IWeeklyDoughClosingReadService
    {
        private readonly TestScenario _scenario;

        public StubWeeklyDoughClosingReadService(TestScenario scenario)
        {
            _scenario = scenario;
        }

        public Task<IReadOnlyList<WeeklyDoughClosingResponse>> GetWeeklyClosingsAsync(
            GetWeeklyClosingsRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_scenario.Closings);
        }

        public Task<WeeklyDoughCarryoverResponse> GetCarryoverForWeekAsync(
            GetWeeklyDoughCarryoverRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_scenario.Carryover);
        }
    }

    private sealed class StubDoughAvailabilityProjectionService : IDoughAvailabilityProjectionService
    {
        private readonly TestScenario _scenario;

        public StubDoughAvailabilityProjectionService(TestScenario scenario)
        {
            _scenario = scenario;
        }

        public Task<DoughAvailabilityProjectionResponse> GetWeeklyAvailabilityAsync(
            DateOnly referenceDate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DoughAvailabilityProjectionResponse
            {
                ReferenceDate = referenceDate,
                WeekStartDate = _scenario.Availability.WeekStartDate,
                WeekEndDate = _scenario.Availability.WeekEndDate,
                HasClosingCarryover = _scenario.Availability.HasClosingCarryover,
                CarryoverSourceWeekStartDate = _scenario.Availability.CarryoverSourceWeekStartDate,
                CarryoverSourceWeekEndDate = _scenario.Availability.CarryoverSourceWeekEndDate,
                CarryoverReadyBalls = _scenario.Availability.CarryoverReadyBalls,
                CarryoverAttentionBalls = _scenario.Availability.CarryoverAttentionBalls,
                CarryoverAvailableBalls = _scenario.Availability.CarryoverAvailableBalls,
                CarryoverMixedButNotBalledLoads = _scenario.Availability.CarryoverMixedButNotBalledLoads,
                PreviousWeekProducedBalls = _scenario.Availability.PreviousWeekProducedBalls,
                PreviousWeekUsedBalls = _scenario.Availability.PreviousWeekUsedBalls,
                PreviousWeekLostBalls = _scenario.Availability.PreviousWeekLostBalls,
                CarryoverClosingNotes = _scenario.Availability.CarryoverClosingNotes,
                ProducedThisWeekBalls = _scenario.Availability.ProducedThisWeekBalls,
                ActualUsedBallsThisWeek = _scenario.Availability.ActualUsedBallsThisWeek,
                LostBallsThisWeek = _scenario.Availability.LostBallsThisWeek,
                AvailableBalls = _scenario.Availability.AvailableBalls,
                RegularReadyBalls = _scenario.Availability.RegularReadyBalls,
                AttentionAvailableBalls = _scenario.Availability.AttentionAvailableBalls,
                MustUseNextDayBalls = _scenario.Availability.MustUseNextDayBalls
            });
        }
    }

    private sealed class StubPrepWeeklyDoughCalendarService : IPrepWeeklyDoughCalendarService
    {
        private readonly TestScenario _scenario;

        public StubPrepWeeklyDoughCalendarService(TestScenario scenario)
        {
            _scenario = scenario;
        }

        public Task<WeeklyDoughCalendarResponse> GetWeekAsync(
            DateOnly referenceDate,
            int historicalWeeksToUse,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_scenario.WeeklyGoal);
        }
    }

    private sealed class StubDoughInventoryImpactReadService : IDoughInventoryImpactReadService
    {
        private readonly TestScenario _scenario;

        public StubDoughInventoryImpactReadService(TestScenario scenario)
        {
            _scenario = scenario;
        }

        public Task<DoughInventoryImpactResponse> GetInventoryImpactAsync(
            GetDoughInventoryImpactRequest request,
            CancellationToken cancellationToken = default)
        {
            var response = _scenario.InventoryImpact;
            response.ReferenceDate = request.ReferenceDate;
            return Task.FromResult(response);
        }
    }

    private sealed class StubPrepItemReadRepository : IPrepItemReadRepository
    {
        private readonly PrepItem _doughItem;

        public StubPrepItemReadRepository(PrepItem doughItem)
        {
            _doughItem = doughItem;
        }

        public Task<IReadOnlyList<PrepItem>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<PrepItem> items = [_doughItem];
            return Task.FromResult(items);
        }

        public Task<PrepItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(id == _doughItem.Id ? _doughItem : null);
        }

        public Task<PrepItem?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                string.Equals(code, _doughItem.Code, StringComparison.OrdinalIgnoreCase)
                    ? _doughItem
                    : null);
        }
    }

    private sealed class StubPrepTaskRepository : IPrepTaskRepository
    {
        private readonly IReadOnlyList<PrepTask> _tasks;

        public StubPrepTaskRepository(IReadOnlyList<PrepTask> tasks)
        {
            _tasks = tasks;
        }

        public Task AddAsync(PrepTask task, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PrepTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tasks.FirstOrDefault(task => task.Id == id));
        }

        public Task<PrepTask?> GetByDoughPrepRecommendationIdAsync(Guid doughPrepRecommendationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tasks.FirstOrDefault(task => task.DoughPrepRecommendationId == doughPrepRecommendationId));
        }

        public Task<IReadOnlyList<PrepTask>> GetDoughTasksByDateAsync(DateOnly taskDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PrepTask>>(_tasks.Where(task => task.TaskDate == taskDate).ToArray());
        }

        public Task<IReadOnlyList<PrepTask>> GetDoughTasksBetweenDatesAsync(
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PrepTask>>(
                _tasks.Where(task => task.TaskDate >= startDate && task.TaskDate <= endDate).ToArray());
        }

        public Task<IReadOnlyList<PrepTask>> SearchDoughTasksAsync(
            DateOnly? taskDate,
            PrepTaskStatus? status,
            ApplicationRole? assignedRole,
            Guid? prepItemId,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<PrepTask> query = _tasks;

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

        public void Remove(PrepTask task) => throw new NotSupportedException();
    }

    private sealed class StubDoughBatchReadRepository : IDoughBatchReadRepository
    {
        private readonly IReadOnlyCollection<DoughBatch> _batches;

        public StubDoughBatchReadRepository(IReadOnlyCollection<DoughBatch> batches)
        {
            _batches = batches;
        }

        public Task<IReadOnlyCollection<DoughBatch>> GetProducedOnOrBeforeAsync(
            DateOnly productionDate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<DoughBatch>>(
                _batches.Where(batch => batch.BatchDate <= productionDate).ToArray());
        }

        public Task<IReadOnlyCollection<DoughBatch>> SearchForCorrectionAsync(
            DateOnly? batchDateFrom,
            DateOnly? batchDateTo,
            bool includeVoided,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<DoughBatch> query = _batches;

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

    private sealed class RecordingWeeklyDoughClosingManagementService : IWeeklyDoughClosingManagementService
    {
        public List<CreateWeeklyDoughClosingRequest> CreateRequests { get; } = [];

        public List<CorrectWeeklyDoughClosingRequest> CorrectRequests { get; } = [];

        public Task<WeeklyDoughClosingResponse> CreateWeeklyClosingAsync(
            CreateWeeklyDoughClosingRequest request,
            CancellationToken cancellationToken = default)
        {
            CreateRequests.Add(request);

            return Task.FromResult(new WeeklyDoughClosingResponse
            {
                Id = Guid.NewGuid(),
                WeekStartDate = request.WeekStartDate,
                WeekEndDate = request.WeekStartDate.AddDays(6),
                NeededBalls = request.NeededBalls,
                ProducedBalls = request.ProducedBalls,
                UsedBalls = request.UsedBalls,
                LostBalls = request.LostBalls,
                LeftoverReadyBalls = request.LeftoverReadyBalls,
                LeftoverAttentionBalls = request.LeftoverAttentionBalls,
                LeftoverMixedLoads = request.LeftoverMixedLoads,
                CarryoverAvailableBalls = request.LeftoverReadyBalls + request.LeftoverAttentionBalls,
                Notes = request.Notes,
                ClosedByUserId = request.ClosedByUserId,
                ClosedAtUtc = request.ClosedAtUtc ?? DateTime.UtcNow
            });
        }

        public Task<WeeklyDoughClosingResponse> CorrectWeeklyClosingAsync(
            CorrectWeeklyDoughClosingRequest request,
            CancellationToken cancellationToken = default)
        {
            CorrectRequests.Add(request);

            return Task.FromResult(new WeeklyDoughClosingResponse
            {
                Id = request.WeeklyDoughClosingId,
                WeekStartDate = new DateOnly(2026, 6, 15),
                WeekEndDate = new DateOnly(2026, 6, 21),
                NeededBalls = request.NeededBalls,
                ProducedBalls = request.ProducedBalls,
                UsedBalls = request.UsedBalls,
                LostBalls = request.LostBalls,
                LeftoverReadyBalls = request.LeftoverReadyBalls,
                LeftoverAttentionBalls = request.LeftoverAttentionBalls,
                LeftoverMixedLoads = request.LeftoverMixedLoads,
                CarryoverAvailableBalls = request.LeftoverReadyBalls + request.LeftoverAttentionBalls,
                Notes = request.Notes,
                ClosedByUserId = request.CorrectedByUserId,
                ClosedAtUtc = request.CorrectedAtUtc ?? DateTime.UtcNow,
                WasCorrected = true,
                CorrectedByUserId = request.CorrectedByUserId,
                CorrectedAtUtc = request.CorrectedAtUtc ?? DateTime.UtcNow,
                CorrectionNote = request.CorrectionNote
            });
        }
    }

    private sealed class RecordingPrepTaskService : IPrepTaskService
    {
        public List<SavePrepTaskRequest> ManualCreateRequests { get; } = [];

        public List<CompletePrepTaskRequest> CompleteRequests { get; } = [];

        public Task<CreatePrepTaskFromRecommendationResponse> CreateFromDoughRecommendationAsync(
            CreatePrepTaskFromRecommendationRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<SavePrepTaskResponse> CreateManualAsync(
            SavePrepTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            ManualCreateRequests.Add(request);
            var prepTaskId = Guid.NewGuid();

            return Task.FromResult(new SavePrepTaskResponse
            {
                PrepTaskId = prepTaskId,
                TaskDate = request.TaskDate,
                PrepItemName = "Dough",
                PrepStationName = "Pizza",
                AssignedRole = request.AssignedRole,
                TaskType = request.TaskType,
                QuantityUnit = request.QuantityUnit,
                QuantityRecommended = request.QuantityValue,
                QuantityRecommendedBallsEquivalent = request.QuantityValue,
                Status = PrepTaskStatus.Pending.ToString(),
                Message = "Prep task created successfully."
            });
        }

        public Task<SavePrepTaskResponse> UpdateManualAsync(
            Guid prepTaskId,
            SavePrepTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CompletePrepTaskResponse> CompleteAsync(
            CompletePrepTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            CompleteRequests.Add(request);

            return Task.FromResult(new CompletePrepTaskResponse
            {
                PrepTaskId = request.PrepTaskId,
                Status = PrepTaskStatus.Completed.ToString(),
                TaskType = PrepTaskType.BallDough.ToString(),
                QuantityUnit = request.QuantityUnit,
                QuantityCompleted = request.QuantityValue,
                QuantityCompletedBallsEquivalent = request.QuantityValue,
                CompletedAtUtc = DateTime.UtcNow,
                Message = "Prep task completed successfully."
            });
        }

        public Task DeleteAsync(Guid prepTaskId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
