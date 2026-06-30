using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Application.Services.AIOrchestration;
using ParlorPrediction.Application.Services.OperationalChat;
using ParlorPrediction.Application.Services.OperationalDrafts;
using ParlorPrediction.Application.Services.OperationalPreview;
using ParlorPrediction.Application.Services.OperationalProjection;
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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
    public async Task OperationalPreviewService_IsDeterministicForSameDraftWithoutDrift()
    {
        await using var harness = CreateHarness();
        var created = await harness.DraftService.CreateWeeklyCorrectionDraftAsync(CreatePrimaryNarrativeRequest());

        var firstPreview = await harness.PreviewService.BuildPreviewAsync(created.Draft.Id);
        var secondPreview = await harness.PreviewService.BuildPreviewAsync(created.Draft.Id);

        Assert.True(firstPreview.UsedPersistedSnapshot);
        Assert.False(firstPreview.StateDriftDetected);
        Assert.Equal(
            JsonSerializer.Serialize(firstPreview, JsonOptions),
            JsonSerializer.Serialize(secondPreview, JsonOptions));
    }

    [Fact]
    public async Task OperationalPreviewService_ReusesPersistedSnapshotWithoutRecomputation()
    {
        await using var harness = CreateHarness();
        var created = await harness.DraftService.CreateWeeklyCorrectionDraftAsync(CreatePrimaryNarrativeRequest());
        var auditEntriesBeforePreview = await harness.AuditRepository.ListByCorrelationIdAsync(created.Draft.CorrelationId);

        var preview = await harness.PreviewService.BuildPreviewAsync(created.Draft.Id);
        var auditEntriesAfterPreview = await harness.AuditRepository.ListByCorrelationIdAsync(created.Draft.CorrelationId);

        Assert.True(preview.UsedPersistedSnapshot);
        Assert.False(preview.StateDriftDetected);
        Assert.Equal(auditEntriesBeforePreview.Count, auditEntriesAfterPreview.Count);
    }

    [Fact]
    public async Task OperationalPreviewService_DetectsDriftAndReplaysSimulation()
    {
        await using var harness = CreateHarness();
        var created = await harness.DraftService.CreateWeeklyCorrectionDraftAsync(CreatePrimaryNarrativeRequest());
        var auditEntriesBeforePreview = await harness.AuditRepository.ListByCorrelationIdAsync(created.Draft.CorrelationId);

        harness.Scenario.WeeklyGoal.ReadyNowBalls = 336;
        harness.Scenario.Availability = CloneAvailabilityWithReadyBalls(harness.Scenario.Availability, 336);
        harness.Scenario.InventoryImpact.ReadyNowBalls = 336;

        var preview = await harness.PreviewService.BuildPreviewAsync(created.Draft.Id);
        var auditEntriesAfterPreview = await harness.AuditRepository.ListByCorrelationIdAsync(created.Draft.CorrelationId);

        Assert.False(preview.UsedPersistedSnapshot);
        Assert.True(preview.StateDriftDetected);
        Assert.Contains(preview.ValidationWarnings, warning => warning.Code == "STATE_DRIFT_DETECTED");
        Assert.Equal(auditEntriesBeforePreview.Count, auditEntriesAfterPreview.Count);
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

    [Fact]
    public async Task OperationalWeekSlice_PersistsSeparatedDraftsAndWeeklyPreviewForJun23ThroughJun28()
    {
        await using var harness = CreateHarness(scenario: TestScenario.CreateOperationalSliceJun23ThroughJun28());

        var result = await harness.WeekSliceService.ExecuteAsync(CreateOperationalSliceRequest());
        var persistedDrafts = await harness.DraftRepository.ListByCorrelationIdAsync(result.CorrelationId);
        var auditEntries = await harness.AuditRepository.ListByCorrelationIdAsync(result.CorrelationId);
        var allDrafts = result.ProductionDrafts
            .Concat(result.DailyClosingDrafts)
            .Concat(result.EventDrafts)
            .Append(result.WeeklyClosingDraft)
            .ToArray();

        Assert.Equal(8, result.ProductionDrafts.Count);
        Assert.Equal(6, result.DailyClosingDrafts.Count);
        Assert.Single(result.EventDrafts);
        Assert.Equal(16, persistedDrafts.Count);
        Assert.Equal(32, auditEntries.Count);
        Assert.All(allDrafts, draft => Assert.Equal(result.CorrelationId, draft.Draft.CorrelationId));
        Assert.All(allDrafts, draft => Assert.Equal(OperationalDraftStatus.Pending, draft.Draft.Status));
        Assert.All(auditEntries, entry => Assert.Equal(result.CorrelationId, entry.CorrelationId));

        var weeklyPreviewPayload = DeserializePayload<WeeklyCorrectionApprovalPayload>(result.WeeklyClosingDraft.Draft.DraftPayloadJson);
        Assert.Equal(new DateOnly(2026, 6, 22), weeklyPreviewPayload.WeekStartDate);
        Assert.Equal(970, weeklyPreviewPayload.UsedBalls);
        Assert.NotEqual(1025, weeklyPreviewPayload.UsedBalls);
        Assert.Contains("970", result.WeeklyClosingDraft.DiffJson, StringComparison.Ordinal);

        var saturdayClosingPayload = DeserializePayload<DailyClosingApprovalPayload>(
            result.DailyClosingDrafts.Single(draft => draft.Draft.SourceText.Contains("2026-06-27", StringComparison.Ordinal)).Draft.DraftPayloadJson);
        Assert.Equal(260, saturdayClosingPayload.ActualUsedBalls);
        Assert.Contains(
            saturdayClosingPayload.UsageBreakdown,
            component => component.Category == "Event" &&
                         component.Balls == 55 &&
                         component.ReferenceName == "Ted Vergakis event");

        var eventPayload = DeserializePayload<RestaurantEventApprovalPayload>(result.EventDrafts.Single().Draft.DraftPayloadJson);
        Assert.Equal(55, eventPayload.EstimatedDoughBalls);
        Assert.Equal(60, eventPayload.PreviousNarrativeDoughBalls);

        Assert.Contains(result.ValidationWarnings, warning => warning.Code == "event-usage-narrative-mismatch" && warning.RequiresHumanReview);
        Assert.Contains(result.ValidationWarnings, warning => warning.Code == "event-usage-already-counted-in-daily-closing");
        Assert.Contains(result.ValidationWarnings, warning => warning.Code == "production-drafts-excluded-from-usage");
        Assert.Contains(result.ValidationWarnings, warning => warning.Code == "make-load-pending-not-ready-now");
        Assert.Contains(result.ValidationWarnings, warning => warning.Code == "weekly-used-balls-overlay" && warning.RequiresHumanReview);
        Assert.DoesNotContain(result.ValidationWarnings, warning => warning.BlocksDraft);
    }

    [Fact]
    public async Task OperationalProjectionService_LeavesReadyNowUnchangedWhileComputingPlanningView()
    {
        await using var harness = CreateHarness(scenario: TestScenario.CreateOperationalSliceJun23ThroughJun28());

        var projection = await harness.ProjectionService.ProjectAsync(CreateProjectionRequest());

        Assert.Equal(168, projection.ReadyNowBalls);
        Assert.Equal(168, projection.BallsReadyForService);
        Assert.Equal(0, projection.ProjectedShortageBalls);
        Assert.Equal(113, projection.ProjectedSurplusBalls);
        Assert.Equal(168, harness.Scenario.WeeklyGoal.ReadyNowBalls);
    }

    [Fact]
    public async Task OperationalProjectionService_AggregatesConsumptionAndMatchesWeeklyClosingConsistency()
    {
        await using var harness = CreateHarness(scenario: TestScenario.CreateOperationalSliceJun23ThroughJun28());
        await SeedWeeklyConsumptionLedgersAsync(harness, includeExternalEvent: true);

        var projection = await harness.ProjectionService.ProjectAsync(CreateProjectionRequest());

        Assert.Equal(915, projection.ConsumptionLedger.SalesBalls);
        Assert.Equal(915, projection.ConsumptionLedger.ServiceUsageBalls);
        Assert.Equal(55, projection.ConsumptionLedger.EventBalls);
        Assert.Equal(55, projection.ConsumptionLedger.PotentialEventDoubleCountBalls);
        Assert.True(projection.WeeklyClosingUsageConsistent);
        Assert.Contains(projection.ValidationWarnings, warning => warning.Code == "projection-event-double-count-risk");
    }

    [Fact]
    public async Task OperationalProjectionService_ReballDoesNotAffectReadyNow()
    {
        await using var harness = CreateHarness();
        await harness.InventoryTransformationLedgerRepository.AddAsync(
            new InventoryTransformationLedger(
                Guid.NewGuid(),
                new DateOnly(2026, 6, 20),
                "ReballRecovered",
                Guid.NewGuid(),
                84,
                0,
                0,
                "Recovered older dough for review."),
            default);
        await harness.DbContext.SaveChangesAsync();

        var projection = await harness.ProjectionService.ProjectAsync(new OperationalProjectionRequest
        {
            ReferenceDate = new DateOnly(2026, 6, 21),
            HistoricalWeeksToUse = 8,
            ActorUserId = "admin-user"
        });

        Assert.Equal(504, projection.ReadyNowBalls);
        Assert.Equal(504, projection.BallsReadyForService);
        Assert.Equal(84, projection.InventoryTransformationLedger.BallsRecovered);
        Assert.Contains(projection.ValidationWarnings, warning => warning.Code == "projection-transformations-informational");
    }

    [Fact]
    public async Task PlanningTools_PreviewOperationalDraft_DoesNotWriteDatabase()
    {
        await using var harness = CreateHarness();
        var created = await harness.DraftService.CreateWeeklyCorrectionDraftAsync(CreatePrimaryNarrativeRequest());
        var draftsBeforePreview = await harness.DraftRepository.ListByCorrelationIdAsync(created.Draft.CorrelationId);
        var auditEntriesBeforePreview = await harness.AuditRepository.ListByCorrelationIdAsync(created.Draft.CorrelationId);

        var preview = await harness.PlanningTools.PreviewOperationalDraftAsync(new PreviewOperationalDraftToolRequest
        {
            DraftId = created.Draft.Id
        });

        var draftsAfterPreview = await harness.DraftRepository.ListByCorrelationIdAsync(created.Draft.CorrelationId);
        var auditEntriesAfterPreview = await harness.AuditRepository.ListByCorrelationIdAsync(created.Draft.CorrelationId);

        Assert.Equal(created.Draft.Id, preview.DraftId);
        Assert.Equal(draftsBeforePreview.Count, draftsAfterPreview.Count);
        Assert.Equal(auditEntriesBeforePreview.Count, auditEntriesAfterPreview.Count);
    }

    [Fact]
    public async Task PlanningTools_DraftProjectionBasedAdjustment_CreatesDraftOnlyAndDoesNotWriteOperationalRecords()
    {
        await using var harness = CreateHarness(scenario: TestScenario.CreateOperationalSliceJun23ThroughJun28());
        await SeedWeeklyConsumptionLedgersAsync(harness, includeExternalEvent: true);

        var result = await harness.PlanningTools.DraftProjectionBasedAdjustmentAsync(new DraftProjectionBasedAdjustmentToolRequest
        {
            ReferenceDate = new DateOnly(2026, 6, 28),
            HistoricalWeeksToUse = 8,
            ActorUserId = "admin-user",
            Notes = "Projection-only adjustment preview."
        });

        Assert.Equal("ProjectionAdjustment", result.Draft.DraftType);
        Assert.Equal(OperationalDraftStatus.Pending, result.Draft.Status);
        Assert.Equal(result.Draft.Id, result.AuditEntry.DraftId);
        Assert.Contains("ProjectedShortageBalls", result.DiffJson, StringComparison.Ordinal);
        Assert.Empty(harness.WeeklyClosingManagementService.CreateRequests);
        Assert.Empty(harness.WeeklyClosingManagementService.CorrectRequests);
        Assert.Empty(harness.DailyClosingManagementService.CreateRequests);
        Assert.Empty(harness.DailyClosingManagementService.CorrectRequests);
        Assert.Empty(harness.RestaurantEventManagementService.CreateRequests);
        Assert.Empty(harness.RestaurantEventManagementService.UpdateRequests);
        Assert.Empty(harness.RestaurantEventManagementService.ActiveStateChanges);
        Assert.Empty(harness.PrepTaskService.ManualCreateRequests);
    }

    [Fact]
    public async Task OperationalDraft_ApprovalIsBlockedWhenPreviewIsHighRisk()
    {
        var scenario = TestScenario.CreateDefault();
        var liveTasks = new List<PrepTask>();
        scenario.Tasks = liveTasks;

        await using var harness = CreateHarness(scenario: scenario);
        var created = await harness.DraftService.CreateDoughTaskDraftAsync(new OperationalDoughTaskDraftRequest
        {
            TaskDate = new DateOnly(2026, 6, 21),
            HistoricalWeeksToUse = 8,
            TaskType = nameof(PrepTaskType.BallDough),
            QuantityValue = 168,
            QuantityUnit = nameof(DoughQuantityUnit.Balls),
            AssignedRole = nameof(ApplicationRole.PizzaMaker),
            AutoCompleteOnApproval = false,
            Notes = "Operational preview high-risk approval guard test.",
            ActorUserId = "admin-user"
        });

        harness.Scenario.WeeklyGoal.ReadyNowBalls = 336;
        harness.Scenario.Availability = CloneAvailabilityWithReadyBalls(harness.Scenario.Availability, 336);
        harness.Scenario.InventoryImpact.ReadyNowBalls = 336;

        liveTasks.Add(PrepTask.Create(
            new DateOnly(2026, 6, 21),
            harness.Scenario.DoughItem.Id,
            harness.Scenario.DoughItem.PrepStationId,
            ApplicationRole.PizzaMaker,
            168,
            taskType: PrepTaskType.BallDough,
            quantityUnit: DoughQuantityUnit.Balls,
            notes: "Duplicate task introduced after draft persistence."));

        var preview = await harness.PreviewService.BuildPreviewAsync(created.Draft.Id);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.DraftService.ApproveDraftAsync(created.Draft.Id, "admin-user"));

        Assert.Equal("High", preview.RiskLevel);
        Assert.Contains(preview.ValidationWarnings, warning => warning.Code == "duplicate-task-draft" && warning.BlocksDraft);
        Assert.True(
            exception.Message.Contains("contains conflicts", StringComparison.Ordinal) ||
            exception.Message.Contains("risk level is High", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OperationalDraft_ApprovalCompletesExistingPendingTaskInsteadOfCreatingDuplicate()
    {
        var scenario = TestScenario.CreateOperationalSliceJun23ThroughJun28();
        var existingTask = PrepTask.Create(
            new DateOnly(2026, 6, 23),
            scenario.DoughItem.Id,
            scenario.DoughItem.PrepStationId,
            ApplicationRole.PizzaMaker,
            1,
            taskType: PrepTaskType.MakeDoughLoad,
            quantityUnit: DoughQuantityUnit.FullLoads,
            notes: "Planned load already present.");
        scenario.Tasks = [existingTask];

        await using var harness = CreateHarness(scenario: scenario);
        var created = await harness.DraftService.CreateDoughTaskDraftAsync(new OperationalDoughTaskDraftRequest
        {
            TaskDate = new DateOnly(2026, 6, 23),
            HistoricalWeeksToUse = 8,
            TaskType = nameof(PrepTaskType.MakeDoughLoad),
            QuantityValue = 1,
            QuantityUnit = nameof(DoughQuantityUnit.FullLoads),
            AssignedRole = nameof(ApplicationRole.PizzaMaker),
            AutoCompleteOnApproval = true,
            CompletionQuantityValue = 1,
            Notes = "Completed MakeDoughLoad.",
            ActorUserId = "manager-user"
        });

        var payload = JsonSerializer.Deserialize<DoughTaskApprovalPayload>(created.Draft.DraftPayloadJson, JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(existingTask.Id, payload!.ExistingPrepTaskId);

        await harness.DraftService.ApproveDraftAsync(created.Draft.Id, "manager-user");

        Assert.Empty(harness.PrepTaskService.ManualCreateRequests);
        Assert.Empty(harness.PrepTaskService.ManualUpdateRequests);
        Assert.Single(harness.PrepTaskService.CompleteRequests);
        Assert.Equal(existingTask.Id, harness.PrepTaskService.CompleteRequests[0].PrepTaskId);
    }

    [Fact]
    public async Task OperationalChat_CreatesSeparateDraftsForProductionAndDailyClosingInSameMessage()
    {
        await using var harness = CreateHarness(scenario: TestScenario.CreateOperationalSliceJun23ThroughJun28());

        var response = await harness.ChatService.SendAsync(new OperationalChatRequest
        {
            SourceText = "Tue: BallDough + MakeDoughLoad\nTue: 90 balls",
            ReferenceDate = new DateOnly(2026, 6, 28),
            TargetWeekStartDate = new DateOnly(2026, 6, 22),
            ActorUserId = "manager-user"
        });

        Assert.False(response.RequiresClarification);
        Assert.Equal(3, response.CreatedDrafts.Count);
        Assert.Contains(response.DetectedIntents, intent => intent.Domain == "Production / Dough Tasks");
        Assert.Contains(response.DetectedIntents, intent => intent.Domain == "Daily Closing / Usage");
        Assert.Equal(2, response.CreatedDrafts.Count(draft => draft.DraftType == "DoughTask"));
        Assert.Single(response.CreatedDrafts, draft => draft.DraftType == "DailyClosing");
        Assert.All(response.CreatedDrafts, draft => Assert.Equal($"/operational-drafts/{draft.DraftId}", draft.ReviewPath));
    }

    [Fact]
    public async Task OperationalChat_StructuredProductionLogWithDayHeadersCreatesProductionDrafts()
    {
        await using var harness = CreateHarness(scenario: TestScenario.CreateOperationalSliceJun23ThroughJun28());

        var response = await harness.ChatService.SendAsync(new OperationalChatRequest
        {
            SourceText =
                """
                Tuesday:
                - Completed BallDough.
                - Completed MakeDoughLoad.

                Wednesday:
                - Completed BallDough.
                - Completed MakeDoughLoad.

                Thursday:
                - Completed BallDough.
                - Completed MakeDoughLoad.

                Friday:
                - Completed BallDough.

                Saturday:
                - Completed MakeDoughLoad.
                """,
            ReferenceDate = new DateOnly(2026, 6, 28),
            TargetWeekStartDate = new DateOnly(2026, 6, 22),
            ActorUserId = "manager-user"
        });

        Assert.False(response.RequiresClarification);
        Assert.Equal(8, response.CreatedDrafts.Count);
        Assert.Equal(8, response.DetectedIntents.Count);
        Assert.All(response.DetectedIntents, intent => Assert.Equal("Production / Dough Tasks", intent.Domain));
        Assert.All(response.CreatedDrafts, draft => Assert.Equal("DoughTask", draft.DraftType));
        Assert.All(response.CreatedDrafts, draft => Assert.Equal($"/operational-drafts/{draft.DraftId}", draft.ReviewPath));
    }

    [Fact]
    public async Task OperationalChat_ShortStructuredProductionLogDoesNotDuplicateDrafts()
    {
        await using var harness = CreateHarness(scenario: TestScenario.CreateOperationalSliceJun23ThroughJun28());

        var response = await harness.ChatService.SendAsync(new OperationalChatRequest
        {
            SourceText =
                """
                Tuesday:
                - Completed BallDough.
                """,
            ReferenceDate = new DateOnly(2026, 6, 28),
            TargetWeekStartDate = new DateOnly(2026, 6, 22),
            ActorUserId = "manager-user"
        });

        var persistedDrafts = await harness.DraftRepository.ListByCorrelationIdAsync(response.CorrelationId);

        Assert.False(response.RequiresClarification);
        Assert.Single(response.DetectedIntents);
        Assert.Single(response.CreatedDrafts);
        Assert.Single(persistedDrafts);
        Assert.Equal("Production / Dough Tasks", response.DetectedIntents[0].Domain);
        Assert.Equal("DoughTask", response.CreatedDrafts[0].DraftType);
        Assert.Equal($"/operational-drafts/{response.CreatedDrafts[0].DraftId}", response.CreatedDrafts[0].ReviewPath);
    }

    [Fact]
    public async Task OperationalChat_ShortStructuredCompletionReusesExistingPendingTaskWithoutDuplicateDrafts()
    {
        var scenario = TestScenario.CreateOperationalSliceJun23ThroughJun28();
        var existingTask = PrepTask.Create(
            new DateOnly(2026, 6, 23),
            scenario.DoughItem.Id,
            scenario.DoughItem.PrepStationId,
            ApplicationRole.PizzaMaker,
            1,
            taskType: PrepTaskType.MakeDoughLoad,
            quantityUnit: DoughQuantityUnit.FullLoads,
            notes: "Planned load already present.");
        scenario.Tasks = [existingTask];

        await using var harness = CreateHarness(scenario: scenario);

        var response = await harness.ChatService.SendAsync(new OperationalChatRequest
        {
            SourceText =
                """
                Tuesday:
                - Completed MakeDoughLoad.
                """,
            ReferenceDate = new DateOnly(2026, 6, 28),
            TargetWeekStartDate = new DateOnly(2026, 6, 22),
            ActorUserId = "manager-user"
        });

        var persistedDrafts = await harness.DraftRepository.ListByCorrelationIdAsync(response.CorrelationId);
        var payload = JsonSerializer.Deserialize<DoughTaskApprovalPayload>(persistedDrafts.Single().DraftPayloadJson, JsonOptions);

        Assert.False(response.RequiresClarification);
        Assert.Single(response.DetectedIntents);
        Assert.Single(response.CreatedDrafts);
        Assert.Single(persistedDrafts);
        Assert.NotNull(payload);
        Assert.Equal(existingTask.Id, payload!.ExistingPrepTaskId);
        Assert.Contains(response.Warnings, warning => warning.Code == "existing-task-will-be-completed");
        Assert.DoesNotContain(response.Warnings, warning => warning.Code == "duplicate-task-draft");
    }

    [Fact]
    public async Task OperationalChat_PreparedWeeklyBlockCreatesSeparatedDrafts()
    {
        await using var harness = CreateHarness(scenario: TestScenario.CreateOperationalSliceJun23ThroughJun28());

        var response = await harness.ChatService.SendAsync(new OperationalChatRequest
        {
            SourceText = CreatePreparedWeeklyOperationalChatBlock(),
            ReferenceDate = new DateOnly(2026, 6, 28),
            TargetWeekStartDate = new DateOnly(2026, 6, 22),
            ActorUserId = "manager-user"
        });

        Assert.False(response.RequiresClarification);
        Assert.Equal(16, response.CreatedDrafts.Count);
        Assert.Contains(response.DetectedIntents, intent => intent.Domain == "Production / Dough Tasks");
        Assert.Contains(response.DetectedIntents, intent => intent.Domain == "Daily Closing / Usage");
        Assert.Contains(response.DetectedIntents, intent => intent.Domain == "External Event");
        Assert.Contains(response.DetectedIntents, intent => intent.Domain == "Weekly Closing Preview");
        Assert.Contains(response.Warnings, warning => warning.Code == "event-usage-narrative-mismatch");
        Assert.Contains(response.Warnings, warning => warning.Code == "make-load-pending-not-ready-now");
    }

    [Fact]
    public async Task OperationalChat_ManagerNarrativeWithDomainHeadersCreatesSeparatedDrafts()
    {
        await using var harness = CreateHarness(scenario: TestScenario.CreateOperationalSliceJun23ThroughJun28());

        var response = await harness.ChatService.SendAsync(new OperationalChatRequest
        {
            SourceText = CreateManagerWeeklyOperationalNarrative(),
            ReferenceDate = new DateOnly(2026, 6, 28),
            TargetWeekStartDate = new DateOnly(2026, 6, 22),
            ActorUserId = "manager-user"
        });

        Assert.False(response.RequiresClarification);
        Assert.Equal(16, response.CreatedDrafts.Count);
        Assert.Contains(response.DetectedIntents, intent => intent.Domain == "Inventory Transformation / Reball");
        Assert.Contains(response.Warnings, warning => warning.Code == "missing-dough-usage-trace");
        Assert.Contains(response.Warnings, warning => warning.Code == "event-usage-narrative-mismatch");
    }

    [Fact]
    public async Task OperationalChat_AmbiguousMessageReturnsClarificationAndNoDraft()
    {
        await using var harness = CreateHarness();

        var response = await harness.ChatService.SendAsync(new OperationalChatRequest
        {
            SourceText = "Please fix what happened Tuesday.",
            ReferenceDate = new DateOnly(2026, 6, 23),
            TargetWeekStartDate = new DateOnly(2026, 6, 22),
            ActorUserId = "manager-user"
        });

        var auditEntries = await harness.AuditRepository.ListByCorrelationIdAsync(response.CorrelationId);

        Assert.True(response.RequiresClarification);
        Assert.Empty(response.CreatedDrafts);
        Assert.Contains(response.Warnings, warning => warning.Code == "clarification-required");
        Assert.NotEmpty(auditEntries);
    }

    [Fact]
    public async Task OperationalChat_DoesNotApproveAnything()
    {
        await using var harness = CreateHarness(scenario: TestScenario.CreateOperationalSliceJun23ThroughJun28());

        var response = await harness.ChatService.SendAsync(new OperationalChatRequest
        {
            SourceText = "Tue: BallDough + MakeDoughLoad\nTue: 90 balls",
            ReferenceDate = new DateOnly(2026, 6, 28),
            TargetWeekStartDate = new DateOnly(2026, 6, 22),
            ActorUserId = "manager-user"
        });

        var drafts = await harness.DraftRepository.ListByCorrelationIdAsync(response.CorrelationId);

        Assert.All(drafts, draft => Assert.Equal(OperationalDraftStatus.Pending, draft.Status));
        Assert.Empty(harness.WeeklyClosingManagementService.CreateRequests);
        Assert.Empty(harness.WeeklyClosingManagementService.CorrectRequests);
        Assert.Empty(harness.DailyClosingManagementService.CreateRequests);
        Assert.Empty(harness.DailyClosingManagementService.CorrectRequests);
        Assert.Empty(harness.RestaurantEventManagementService.CreateRequests);
        Assert.Empty(harness.RestaurantEventManagementService.UpdateRequests);
        Assert.Empty(harness.PrepTaskService.ManualCreateRequests);
    }

    [Fact]
    public async Task OperationalChat_ResponseIncludesReviewLinks()
    {
        await using var harness = CreateHarness(scenario: TestScenario.CreateOperationalSliceJun23ThroughJun28());

        var response = await harness.ChatService.SendAsync(new OperationalChatRequest
        {
            SourceText = "Tue: 90 balls",
            ReferenceDate = new DateOnly(2026, 6, 28),
            TargetWeekStartDate = new DateOnly(2026, 6, 22),
            ActorUserId = "manager-user"
        });

        var createdDraft = Assert.Single(response.CreatedDrafts);
        Assert.Equal("DailyClosing", createdDraft.DraftType);
        Assert.Equal($"/operational-drafts/{createdDraft.DraftId}", createdDraft.ReviewPath);
    }

    [Fact]
    public async Task OperationalProjectionLedgers_PersistAcrossRestart()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString("N");

        await using (var firstHarness = CreateHarness(databaseRoot, databaseName, TestScenario.CreateOperationalSliceJun23ThroughJun28()))
        {
            await SeedWeeklyConsumptionLedgersAsync(firstHarness, includeExternalEvent: true);
            await firstHarness.ProductionLedgerRepository.AddAsync(
                new ProductionLedger(
                    Guid.NewGuid(),
                    new DateOnly(2026, 6, 24),
                    "BallDough",
                    Guid.NewGuid(),
                    0,
                    168,
                    0,
                    0,
                    "Persisted production history."),
                default);
            await firstHarness.DbContext.SaveChangesAsync();
        }

        await using var secondHarness = CreateHarness(databaseRoot, databaseName, TestScenario.CreateOperationalSliceJun23ThroughJun28());
        var productionEntries = await secondHarness.ProductionLedgerRepository.ListByOccurredOnRangeAsync(
            new DateOnly(2026, 6, 23),
            new DateOnly(2026, 6, 28));
        var consumptionEntries = await secondHarness.ConsumptionLedgerRepository.ListByOccurredOnRangeAsync(
            new DateOnly(2026, 6, 23),
            new DateOnly(2026, 6, 28));

        Assert.Single(productionEntries);
        Assert.Equal(7, consumptionEntries.Count);
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

    private static OperationalWeekSliceRequest CreateOperationalSliceRequest(Guid? correlationId = null)
    {
        return new OperationalWeekSliceRequest
        {
            CorrelationId = correlationId,
            WeekStartDate = new DateOnly(2026, 6, 23),
            ReferenceDate = new DateOnly(2026, 6, 28),
            HistoricalWeeksToUse = 8,
            ActorUserId = "admin-user",
            WeeklyClosingNotes = "Weekly closing preview for the Tue Jun 23, 2026 through Sun Jun 28, 2026 operational slice.",
            ProductionDrafts =
            [
                CreateProductionDraft(new DateOnly(2026, 6, 23), nameof(PrepTaskType.BallDough), 168, nameof(DoughQuantityUnit.Balls)),
                CreateProductionDraft(new DateOnly(2026, 6, 23), nameof(PrepTaskType.MakeDoughLoad), 1, nameof(DoughQuantityUnit.FullLoads)),
                CreateProductionDraft(new DateOnly(2026, 6, 24), nameof(PrepTaskType.BallDough), 168, nameof(DoughQuantityUnit.Balls)),
                CreateProductionDraft(new DateOnly(2026, 6, 24), nameof(PrepTaskType.MakeDoughLoad), 1, nameof(DoughQuantityUnit.FullLoads)),
                CreateProductionDraft(new DateOnly(2026, 6, 25), nameof(PrepTaskType.BallDough), 168, nameof(DoughQuantityUnit.Balls)),
                CreateProductionDraft(new DateOnly(2026, 6, 25), nameof(PrepTaskType.MakeDoughLoad), 1, nameof(DoughQuantityUnit.FullLoads)),
                CreateProductionDraft(new DateOnly(2026, 6, 26), nameof(PrepTaskType.BallDough), 168, nameof(DoughQuantityUnit.Balls)),
                CreateProductionDraft(new DateOnly(2026, 6, 27), nameof(PrepTaskType.MakeDoughLoad), 1, nameof(DoughQuantityUnit.FullLoads))
            ],
            DailyClosingDrafts =
            [
                CreateDailyClosingDraft(new DateOnly(2026, 6, 23), 90, [new OperationalUsageComponent("DailyClosing", 90)]),
                CreateDailyClosingDraft(new DateOnly(2026, 6, 24), 80, [new OperationalUsageComponent("DailyClosing", 80)]),
                CreateDailyClosingDraft(new DateOnly(2026, 6, 25), 280,
                [
                    new OperationalUsageComponent("Farmers", 200),
                    new OperationalUsageComponent("Restaurant", 80)
                ]),
                CreateDailyClosingDraft(new DateOnly(2026, 6, 26), 195,
                [
                    new OperationalUsageComponent("Farmers", 45),
                    new OperationalUsageComponent("Restaurant", 150)
                ]),
                CreateDailyClosingDraft(new DateOnly(2026, 6, 27), 260,
                [
                    new OperationalUsageComponent("Farmers", 140),
                    new OperationalUsageComponent("Event", 55, "Ted Vergakis event"),
                    new OperationalUsageComponent("Restaurant", 65)
                ]),
                CreateDailyClosingDraft(new DateOnly(2026, 6, 28), 65, [new OperationalUsageComponent("DailyClosing", 65)])
            ],
            EventDrafts =
            [
                new OperationalEventDraftRequest
                {
                    EventDate = new DateOnly(2026, 6, 27),
                    Name = "Ted Vergakis event",
                    EstimatedDoughBalls = 55,
                    ExpectedPeopleMinimum = 51,
                    ExpectedPeopleMaximum = 75,
                    PreviousNarrativeDoughBalls = 60,
                    Notes = "Current operational slice says 55 balls for Ted Vergakis event.",
                    ActorUserId = "admin-user"
                }
            ]
        };
    }

    private static OperationalProjectionRequest CreateProjectionRequest(Guid? correlationId = null)
    {
        return new OperationalProjectionRequest
        {
            CorrelationId = correlationId,
            ReferenceDate = new DateOnly(2026, 6, 28),
            HistoricalWeeksToUse = 8,
            Notes = "Projection request for weekly planning validation.",
            ActorUserId = "admin-user"
        };
    }

    private static string CreatePreparedWeeklyOperationalChatBlock()
    {
        return
            """
            Tue 2026-06-23: Completed BallDough.
            Tue 2026-06-23: Completed MakeDoughLoad.

            Wed 2026-06-24: Completed BallDough.
            Wed 2026-06-24: Completed MakeDoughLoad.

            Thu 2026-06-25: Completed BallDough.
            Thu 2026-06-25: Completed MakeDoughLoad.

            Fri 2026-06-26: Completed BallDough.

            Sat 2026-06-27: Completed MakeDoughLoad.

            Tue 2026-06-23: 90 balls.
            Wed 2026-06-24: 80 balls.
            Thu 2026-06-25: Farmers 200 + Restaurant 80.
            Fri 2026-06-26: Farmers 45 + Restaurant 150.
            Sat 2026-06-27: Farmers 140 + Restaurant 65.
            Sun 2026-06-28: 65 balls.

            Sat 2026-06-27: Ted Vergakis event - 51-75 people - 55 balls used. Previous narrative said 60 balls.

            Weekly closing preview for 2026-06-22 through 2026-06-28. Regular usage excluding event should be 915 balls. External event usage should be 55 balls. Total used balls should be 970. Saturday event usage is already included in Saturday operations and must not be double counted. MakeDoughLoad does not increase ReadyNow until BallDough is completed.
            """;
    }

    private static string CreateManagerWeeklyOperationalNarrative()
    {
        return
            """
            DOMAIN 1 — Production / Dough Tasks

            Tuesday 06/23/2026:
            - Completed BallDough.
            - Completed MakeDoughLoad.

            Wednesday 06/24/2026:
            - Completed BallDough.
            - Completed MakeDoughLoad.

            Thursday 06/25/2026:
            - Completed BallDough.
            - Completed MakeDoughLoad.

            Friday 06/26/2026:
            - Completed BallDough.

            Saturday 06/27/2026:
            - Completed MakeDoughLoad.

            DOMAIN 2 — Daily Closing / Consumption

            Tuesday 06/23/2026:
            - Used 90 balls.

            Wednesday 06/24/2026:
            - Used 80 balls.

            Thursday 06/25/2026:
            - Farmers Market: 200 balls.
            - Restaurant: 80 balls.
            - Total: 280 balls.

            Friday 06/26/2026:
            - Farmers Market: 45 balls.
            - Restaurant: 150 balls.
            - Total: 195 balls.

            Saturday 06/27/2026:
            - Farmers Market: 140 balls.
            - Restaurant: 65 balls.
            - External event usage is handled separately in Domain 3.
            - Saturday regular usage excluding event: 205 balls.

            Sunday 06/28/2026:
            - Restaurant/general: 65 balls.

            DOMAIN 3 — External Event

            Saturday 06/27/2026:
            - Event name: Ted Vergakis event.
            - Attendance: 51-75 people.
            - Dough used: 55 balls.
            - Previous narrative mentioned 60 balls for this event.

            DOMAIN 4 — Inventory Transformation / Reball

            - No reballed dough was reported.
            - No discarded dough was reported.
            - No inventory correction was reported.
            - DoughUsageTrace/source-date tracking is still missing and should be provided later.

            DOMAIN 5 — Weekly Closing Preview

            Prepare a Weekly Closing preview for week 06/22/2026 through 06/28/2026.
            Regular usage excluding event: 915 balls.
            External event usage: 55 balls.
            Total used balls: 970 balls.
            Saturday event usage is already included in Saturday operations and must not be double counted.
            MakeDoughLoad does not increase ReadyNow until BallDough is completed.
            """;
    }

    private static OperationalDoughTaskDraftRequest CreateProductionDraft(
        DateOnly taskDate,
        string taskType,
        int quantityValue,
        string quantityUnit)
    {
        return new OperationalDoughTaskDraftRequest
        {
            TaskDate = taskDate,
            TaskType = taskType,
            QuantityValue = quantityValue,
            QuantityUnit = quantityUnit,
            AssignedRole = nameof(ApplicationRole.PizzaMaker),
            AutoCompleteOnApproval = false,
            Notes = $"Structured production draft for {taskType} on {taskDate:yyyy-MM-dd}.",
            ActorUserId = "admin-user"
        };
    }

    private static OperationalDailyClosingDraftRequest CreateDailyClosingDraft(
        DateOnly closingDate,
        int actualUsedBalls,
        IReadOnlyList<OperationalUsageComponent> usageBreakdown)
    {
        return new OperationalDailyClosingDraftRequest
        {
            ClosingDate = closingDate,
            ActualUsedBalls = actualUsedBalls,
            UsageBreakdown = usageBreakdown,
            Notes = $"Structured daily closing draft for {closingDate:yyyy-MM-dd}.",
            ActorUserId = "admin-user"
        };
    }

    private static T DeserializePayload<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name} test payload.");
    }

    private static async Task SeedWeeklyConsumptionLedgersAsync(
        OperationalTestHarness harness,
        bool includeExternalEvent)
    {
        var entries = new List<ConsumptionLedger>
        {
            new(Guid.NewGuid(), new DateOnly(2026, 6, 23), "DailyClosing", Guid.Parse("3dc00b33-cd66-4f35-b715-63490e17a001"), 90, 0, 90, true, "Tuesday closing."),
            new(Guid.NewGuid(), new DateOnly(2026, 6, 24), "DailyClosing", Guid.Parse("3dc00b33-cd66-4f35-b715-63490e17a002"), 80, 0, 80, true, "Wednesday closing."),
            new(Guid.NewGuid(), new DateOnly(2026, 6, 25), "DailyClosing", Guid.Parse("3dc00b33-cd66-4f35-b715-63490e17a003"), 280, 0, 280, true, "Thursday closing."),
            new(Guid.NewGuid(), new DateOnly(2026, 6, 26), "DailyClosing", Guid.Parse("3dc00b33-cd66-4f35-b715-63490e17a004"), 195, 0, 195, true, "Friday closing."),
            new(Guid.NewGuid(), new DateOnly(2026, 6, 27), "DailyClosing", Guid.Parse("3dc00b33-cd66-4f35-b715-63490e17a005"), 205, 0, 205, true, "Saturday closing without double-counting event."),
            new(Guid.NewGuid(), new DateOnly(2026, 6, 28), "DailyClosing", Guid.Parse("3dc00b33-cd66-4f35-b715-63490e17a006"), 65, 0, 65, true, "Sunday closing.")
        };

        if (includeExternalEvent)
        {
            entries.Add(
                new ConsumptionLedger(
                    Guid.NewGuid(),
                    new DateOnly(2026, 6, 27),
                    "RestaurantEvent",
                    Guid.Parse("3dc00b33-cd66-4f35-b715-63490e17a007"),
                    0,
                    55,
                    0,
                    true,
                    "Ted Vergakis event demand."));
        }

        foreach (var entry in entries)
        {
            await harness.ConsumptionLedgerRepository.AddAsync(entry);
        }

        await harness.DbContext.SaveChangesAsync();
    }

    private static DoughAvailabilityProjectionResponse CloneAvailabilityWithReadyBalls(
        DoughAvailabilityProjectionResponse availability,
        int readyBalls)
    {
        return new DoughAvailabilityProjectionResponse
        {
            ReferenceDate = availability.ReferenceDate,
            WeekStartDate = availability.WeekStartDate,
            WeekEndDate = availability.WeekEndDate,
            HasClosingCarryover = availability.HasClosingCarryover,
            CarryoverSourceWeekStartDate = availability.CarryoverSourceWeekStartDate,
            CarryoverSourceWeekEndDate = availability.CarryoverSourceWeekEndDate,
            CarryoverReadyBalls = availability.CarryoverReadyBalls,
            CarryoverAttentionBalls = availability.CarryoverAttentionBalls,
            CarryoverAvailableBalls = availability.CarryoverAvailableBalls,
            CarryoverMixedButNotBalledLoads = availability.CarryoverMixedButNotBalledLoads,
            PreviousWeekProducedBalls = availability.PreviousWeekProducedBalls,
            PreviousWeekUsedBalls = availability.PreviousWeekUsedBalls,
            PreviousWeekLostBalls = availability.PreviousWeekLostBalls,
            CarryoverClosingNotes = availability.CarryoverClosingNotes,
            ProducedThisWeekBalls = availability.ProducedThisWeekBalls,
            ActualUsedBallsThisWeek = availability.ActualUsedBallsThisWeek,
            LostBallsThisWeek = availability.LostBallsThisWeek,
            AvailableBalls = readyBalls,
            RegularReadyBalls = readyBalls,
            AttentionAvailableBalls = availability.AttentionAvailableBalls,
            MustUseNextDayBalls = availability.MustUseNextDayBalls
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
        var productionLedgerRepository = new ProductionLedgerRepository(dbContext);
        var consumptionLedgerRepository = new ConsumptionLedgerRepository(dbContext);
        var inventoryTransformationLedgerRepository = new InventoryTransformationLedgerRepository(dbContext);
        var dailyClosingReadService = new StubDailyDoughClosingReadService(scenario);
        var dailyClosingManagementService = new RecordingDailyDoughClosingManagementService();
        var restaurantEventManagementService = new RecordingRestaurantEventManagementService(scenario.ExistingRestaurantEvents);
        var weeklyClosingReadService = new StubWeeklyDoughClosingReadService(scenario);
        var weeklyClosingManagementService = new RecordingWeeklyDoughClosingManagementService();
        var prepTaskService = new RecordingPrepTaskService();
        var doughAvailabilityProjectionService = new StubDoughAvailabilityProjectionService(scenario);
        var doughInventoryImpactReadService = new StubDoughInventoryImpactReadService(scenario);
        var prepWeeklyDoughCalendarService = new StubPrepWeeklyDoughCalendarService(scenario);
        var projectionService = new OperationalProjectionService(
            consumptionLedgerRepository,
            dailyClosingReadService,
            doughAvailabilityProjectionService,
            doughInventoryImpactReadService,
            inventoryTransformationLedgerRepository,
            prepWeeklyDoughCalendarService,
            productionLedgerRepository);
        var simulationService = new OperationalSimulationService(
            dailyClosingReadService,
            doughAvailabilityProjectionService,
            new StubDoughBatchReadRepository(scenario.Batches),
            doughInventoryImpactReadService,
            auditRepository,
            draftRepository,
            new OperationalIntentClassifier(),
            projectionService,
            new StubPrepItemReadRepository(scenario.DoughItem),
            new StubPrepTaskRepository(scenario.Tasks),
            prepWeeklyDoughCalendarService,
            restaurantEventManagementService,
            unitOfWork,
            weeklyClosingReadService);
        var previewService = new OperationalPreviewService(
            draftRepository,
            projectionService,
            simulationService);
        var draftService = new OperationalDraftService(
            auditRepository,
            draftRepository,
            dailyClosingManagementService,
            previewService,
            simulationService,
            prepTaskService,
            restaurantEventManagementService,
            unitOfWork,
            weeklyClosingManagementService);
        var weekSliceService = new OperationalWeekSliceService(draftService);
        var chatService = new OperationalChatService(
            draftService,
            new OperationalIntentClassifier(),
            previewService,
            weekSliceService,
            simulationService);
        var weeklyGoalExplanationService = new OperationalWeeklyGoalExplanationService(
            doughAvailabilityProjectionService,
            doughInventoryImpactReadService,
            prepWeeklyDoughCalendarService);
        var planningTools = new PlanningTools(
            draftService,
            previewService,
            simulationService,
            weeklyGoalExplanationService,
            new McpToolAllowlist());

        return new OperationalTestHarness(
            dbContext,
            scenario,
            draftRepository,
            auditRepository,
            productionLedgerRepository,
            consumptionLedgerRepository,
            inventoryTransformationLedgerRepository,
            projectionService,
            simulationService,
            previewService,
            draftService,
            chatService,
            weekSliceService,
            planningTools,
            dailyClosingReadService,
            dailyClosingManagementService,
            restaurantEventManagementService,
            weeklyClosingReadService,
            weeklyClosingManagementService,
            prepTaskService);
    }

    private sealed class OperationalTestHarness : IAsyncDisposable, IDisposable
    {
        public OperationalTestHarness(
            ParlorPredictionDbContext dbContext,
            TestScenario scenario,
            IOperationalDraftRepository draftRepository,
            IOperationalAuditEntryRepository auditRepository,
            IProductionLedgerRepository productionLedgerRepository,
            IConsumptionLedgerRepository consumptionLedgerRepository,
            IInventoryTransformationLedgerRepository inventoryTransformationLedgerRepository,
            OperationalProjectionService projectionService,
            OperationalSimulationService simulationService,
            OperationalPreviewService previewService,
            OperationalDraftService draftService,
            OperationalChatService chatService,
            OperationalWeekSliceService weekSliceService,
            PlanningTools planningTools,
            StubDailyDoughClosingReadService dailyClosingReadService,
            RecordingDailyDoughClosingManagementService dailyClosingManagementService,
            RecordingRestaurantEventManagementService restaurantEventManagementService,
            StubWeeklyDoughClosingReadService weeklyClosingReadService,
            RecordingWeeklyDoughClosingManagementService weeklyClosingManagementService,
            RecordingPrepTaskService prepTaskService)
        {
            DbContext = dbContext;
            Scenario = scenario;
            DraftRepository = draftRepository;
            AuditRepository = auditRepository;
            ProductionLedgerRepository = productionLedgerRepository;
            ConsumptionLedgerRepository = consumptionLedgerRepository;
            InventoryTransformationLedgerRepository = inventoryTransformationLedgerRepository;
            ProjectionService = projectionService;
            SimulationService = simulationService;
            PreviewService = previewService;
            DraftService = draftService;
            ChatService = chatService;
            WeekSliceService = weekSliceService;
            PlanningTools = planningTools;
            DailyClosingReadService = dailyClosingReadService;
            DailyClosingManagementService = dailyClosingManagementService;
            RestaurantEventManagementService = restaurantEventManagementService;
            WeeklyClosingReadService = weeklyClosingReadService;
            WeeklyClosingManagementService = weeklyClosingManagementService;
            PrepTaskService = prepTaskService;
        }

        public ParlorPredictionDbContext DbContext { get; }

        public TestScenario Scenario { get; }

        public IOperationalDraftRepository DraftRepository { get; }

        public IOperationalAuditEntryRepository AuditRepository { get; }

        public IProductionLedgerRepository ProductionLedgerRepository { get; }

        public IConsumptionLedgerRepository ConsumptionLedgerRepository { get; }

        public IInventoryTransformationLedgerRepository InventoryTransformationLedgerRepository { get; }

        public OperationalProjectionService ProjectionService { get; }

        public OperationalSimulationService SimulationService { get; }

        public OperationalPreviewService PreviewService { get; }

        public OperationalDraftService DraftService { get; }

        public OperationalChatService ChatService { get; }

        public OperationalWeekSliceService WeekSliceService { get; }

        public PlanningTools PlanningTools { get; }

        public StubDailyDoughClosingReadService DailyClosingReadService { get; }

        public RecordingDailyDoughClosingManagementService DailyClosingManagementService { get; }

        public RecordingRestaurantEventManagementService RestaurantEventManagementService { get; }

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

        public DailyClosingWeekSummaryResponse DailySummary { get; set; } = new();

        public DailyClosingOperationalInsightsResponse DailyInsights { get; set; } = new();

        public IReadOnlyList<RestaurantEventListItemResponse> ExistingRestaurantEvents { get; set; } = [];

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
                DailySummary = new DailyClosingWeekSummaryResponse
                {
                    ReferenceDate = new DateOnly(2026, 6, 21),
                    WeekStartDate = new DateOnly(2026, 6, 16),
                    WeekEndDate = new DateOnly(2026, 6, 21)
                },
                DailyInsights = new DailyClosingOperationalInsightsResponse
                {
                    ReferenceDate = new DateOnly(2026, 6, 21),
                    WeekStartDate = new DateOnly(2026, 6, 16),
                    WeekEndDate = new DateOnly(2026, 6, 21),
                    CurrentAvailableBalls = 504,
                    TotalActualUsedBalls = 1010
                },
                DoughItem = new PrepItem(prepItemId, prepStationId, "Dough", "DOUGH", "Operational dough item")
            };
        }

        public static TestScenario CreateOperationalSliceJun23ThroughJun28()
        {
            var prepStationId = Guid.Parse("4d88d1d4-621c-41d4-aa3f-b6a9124f82ab");
            var prepItemId = Guid.Parse("f1aeedfd-b13f-4b39-af80-6e4d34bd71d5");

            return new TestScenario
            {
                Closings = [],
                Carryover = new WeeklyDoughCarryoverResponse
                {
                    TargetWeekStartDate = new DateOnly(2026, 6, 22),
                    TargetWeekEndDate = new DateOnly(2026, 6, 28),
                    HasClosingCarryover = true,
                    SourceWeekStartDate = new DateOnly(2026, 6, 15),
                    SourceWeekEndDate = new DateOnly(2026, 6, 21),
                    CarryoverReadyBalls = 168,
                    CarryoverAttentionBalls = 0,
                    CarryoverAvailableBalls = 168,
                    MixedButNotBalledLoads = 0,
                    PreviousWeekProducedBalls = 1008,
                    PreviousWeekUsedBalls = 1010,
                    PreviousWeekLostBalls = 0,
                    ClosingNotes = "Carryover aligned from the prior weekly closing."
                },
                Availability = new DoughAvailabilityProjectionResponse
                {
                    ReferenceDate = new DateOnly(2026, 6, 28),
                    WeekStartDate = new DateOnly(2026, 6, 23),
                    WeekEndDate = new DateOnly(2026, 6, 28),
                    HasClosingCarryover = true,
                    CarryoverSourceWeekStartDate = new DateOnly(2026, 6, 15),
                    CarryoverSourceWeekEndDate = new DateOnly(2026, 6, 21),
                    CarryoverReadyBalls = 168,
                    CarryoverAvailableBalls = 168,
                    CarryoverMixedButNotBalledLoads = 0,
                    PreviousWeekProducedBalls = 1008,
                    PreviousWeekUsedBalls = 1010,
                    PreviousWeekLostBalls = 0,
                    ProducedThisWeekBalls = 840,
                    ActualUsedBallsThisWeek = 915,
                    LostBallsThisWeek = 0,
                    AvailableBalls = 168,
                    RegularReadyBalls = 168,
                    AttentionAvailableBalls = 0,
                    MustUseNextDayBalls = 0
                },
                WeeklyGoal = new WeeklyDoughCalendarResponse
                {
                    WeekStartDate = new DateOnly(2026, 6, 23),
                    WeekEndDate = new DateOnly(2026, 6, 28),
                    WeekTotalNeededBalls = 970,
                    ReadyNowBalls = 168,
                    MixedButNotBalledBalls = 0,
                    MixedButNotBalledLoads = 0,
                    StillFermentingBalls = 0,
                    CarryoverMixedButNotBalledPotentialBalls = 0,
                    FutureBalls = 0,
                    PreviousWeekFinishedBalls = 504,
                    StillMissingThisWeekBalls = 0,
                    ProducedThisWeekBalls = 840,
                    ActualUsedBallsThisWeek = 915,
                    CarryoverAvailableBalls = 168,
                    CarryoverMixedButNotBalledLoads = 0
                },
                InventoryImpact = new DoughInventoryImpactResponse
                {
                    ReferenceDate = new DateOnly(2026, 6, 28),
                    WeekStartDate = new DateOnly(2026, 6, 23),
                    WeekEndDate = new DateOnly(2026, 6, 28),
                    WeeklyGoalBalls = 970,
                    ReadyNowBalls = 168,
                    StillMissingBalls = 0,
                    MixedButNotBalledBalls = 0,
                    FutureBalls = 0,
                    UsedTodayBalls = 65,
                    LostOrDiscardedBalls = 0,
                    RemainingTrackedBalls = 168,
                    RemainingSources = []
                },
                DailySummary = new DailyClosingWeekSummaryResponse
                {
                    ReferenceDate = new DateOnly(2026, 6, 28),
                    WeekStartDate = new DateOnly(2026, 6, 23),
                    WeekEndDate = new DateOnly(2026, 6, 28),
                    Days =
                    [
                        new DailyClosingWeekDayResponse { Date = new DateOnly(2026, 6, 23), ForecastNeededBalls = 90, IsClosed = false, IsFuture = false, IsToday = false },
                        new DailyClosingWeekDayResponse { Date = new DateOnly(2026, 6, 24), ForecastNeededBalls = 80, IsClosed = false, IsFuture = false, IsToday = false },
                        new DailyClosingWeekDayResponse { Date = new DateOnly(2026, 6, 25), ForecastNeededBalls = 280, IsClosed = false, IsFuture = false, IsToday = false },
                        new DailyClosingWeekDayResponse { Date = new DateOnly(2026, 6, 26), ForecastNeededBalls = 195, IsClosed = false, IsFuture = false, IsToday = false },
                        new DailyClosingWeekDayResponse { Date = new DateOnly(2026, 6, 27), ForecastNeededBalls = 260, IsClosed = false, IsFuture = false, IsToday = false },
                        new DailyClosingWeekDayResponse { Date = new DateOnly(2026, 6, 28), ForecastNeededBalls = 65, IsClosed = false, IsFuture = false, IsToday = true }
                    ],
                    TotalForecastBalls = 970,
                    TotalActualUsedBalls = 915,
                    AccumulatedVariance = -55,
                    AccumulatedShortage = 55,
                    ClosedDaysCount = 0
                },
                DailyInsights = new DailyClosingOperationalInsightsResponse
                {
                    ReferenceDate = new DateOnly(2026, 6, 28),
                    WeekStartDate = new DateOnly(2026, 6, 23),
                    WeekEndDate = new DateOnly(2026, 6, 28),
                    TotalActualUsedBalls = 915,
                    CurrentAvailableBalls = 168,
                    MixedButNotBalledBalls = 0,
                    StillFermentingBalls = 0,
                    RemainingForecastNeed = 0,
                    AdjustedRemainingForecastNeed = 0,
                    TraceReconciliationDifferenceBalls = 0,
                    Recommendation = "Weekly preview should reconcile daily closing drafts before approval."
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

    private sealed class StubDailyDoughClosingReadService : IDailyDoughClosingReadService
    {
        private readonly TestScenario _scenario;

        public StubDailyDoughClosingReadService(TestScenario scenario)
        {
            _scenario = scenario;
        }

        public Task<DailyDoughClosingResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var match = _scenario.DailySummary.Days.FirstOrDefault(day => day.DailyClosingId == id);
            if (match is null)
            {
                return Task.FromResult<DailyDoughClosingResponse?>(null);
            }

            return Task.FromResult<DailyDoughClosingResponse?>(new DailyDoughClosingResponse
            {
                Id = id,
                ClosingDate = match.Date,
                WeekStartDate = _scenario.DailySummary.WeekStartDate,
                ForecastNeededBalls = match.ForecastNeededBalls,
                ActualUsedBalls = match.ActualUsedBalls ?? 0,
                DailyVariance = match.DailyVariance ?? (match.ForecastNeededBalls - (match.ActualUsedBalls ?? 0)),
                Notes = match.Notes,
                ClosedByUserId = "stub-user",
                ClosedAtUtc = DateTime.UtcNow
            });
        }

        public Task<DailyClosingWeekSummaryResponse> GetWeekSummaryAsync(
            GetDailyClosingWeekSummaryRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_scenario.DailySummary);
        }

        public Task<DailyClosingOperationalInsightsResponse> GetOperationalInsightsAsync(
            GetDailyClosingWeekSummaryRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_scenario.DailyInsights);
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
            bool includeCancelled = false,
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

    private sealed class RecordingDailyDoughClosingManagementService : IDailyDoughClosingManagementService
    {
        public List<CreateDailyDoughClosingRequest> CreateRequests { get; } = [];

        public List<CorrectDailyDoughClosingRequest> CorrectRequests { get; } = [];

        public Task<DailyDoughClosingResponse> CreateDailyClosingAsync(
            CreateDailyDoughClosingRequest request,
            CancellationToken cancellationToken = default)
        {
            CreateRequests.Add(request);

            return Task.FromResult(new DailyDoughClosingResponse
            {
                Id = Guid.NewGuid(),
                ClosingDate = request.ClosingDate,
                WeekStartDate = request.ClosingDate.AddDays(-(((int)request.ClosingDate.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7)),
                ForecastNeededBalls = request.ForecastNeededBalls,
                ActualUsedBalls = request.ActualUsedBalls,
                DailyVariance = request.ForecastNeededBalls - request.ActualUsedBalls,
                Notes = request.Notes,
                ClosedByUserId = request.ClosedByUserId,
                ClosedAtUtc = request.ClosedAtUtc ?? DateTime.UtcNow
            });
        }

        public Task<DailyDoughClosingResponse> CorrectDailyClosingAsync(
            CorrectDailyDoughClosingRequest request,
            CancellationToken cancellationToken = default)
        {
            CorrectRequests.Add(request);

            return Task.FromResult(new DailyDoughClosingResponse
            {
                Id = request.DailyDoughClosingId,
                ClosingDate = new DateOnly(2026, 6, 27),
                WeekStartDate = new DateOnly(2026, 6, 22),
                ForecastNeededBalls = request.ForecastNeededBalls,
                ActualUsedBalls = request.ActualUsedBalls,
                DailyVariance = request.ForecastNeededBalls - request.ActualUsedBalls,
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

    private sealed class RecordingRestaurantEventManagementService : IRestaurantEventManagementService
    {
        private readonly List<RestaurantEventListItemResponse> _events;

        public RecordingRestaurantEventManagementService(IReadOnlyList<RestaurantEventListItemResponse> existingEvents)
        {
            _events = [.. existingEvents];
        }

        public List<SaveRestaurantEventRequest> CreateRequests { get; } = [];

        public List<(Guid Id, SaveRestaurantEventRequest Request)> UpdateRequests { get; } = [];

        public List<(Guid Id, bool IsActive)> ActiveStateChanges { get; } = [];

        public Task<IReadOnlyList<RestaurantEventListItemResponse>> SearchAsync(
            DateOnly? fromDate,
            DateOnly? toDate,
            string? term,
            bool activeOnly,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<RestaurantEventListItemResponse> query = _events;

            if (fromDate.HasValue)
            {
                query = query.Where(item => item.EventDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(item => item.EventDate <= toDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(item => item.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (activeOnly)
            {
                query = query.Where(item => item.IsActive);
            }

            return Task.FromResult<IReadOnlyList<RestaurantEventListItemResponse>>(query.ToArray());
        }

        public Task<RestaurantEventDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var match = _events.FirstOrDefault(item => item.Id == id);
            if (match is null)
            {
                return Task.FromResult<RestaurantEventDetailResponse?>(null);
            }

            return Task.FromResult<RestaurantEventDetailResponse?>(new RestaurantEventDetailResponse
            {
                Id = match.Id,
                Name = match.Name,
                EventDate = match.EventDate,
                EstimatedPizzas = match.EstimatedPizzas,
                EstimatedDoughBalls = match.EstimatedDoughBalls,
                AllowShortFermentation = match.AllowShortFermentation,
                Notes = match.Notes,
                IsActive = match.IsActive
            });
        }

        public Task<Guid> CreateAsync(SaveRestaurantEventRequest request, CancellationToken cancellationToken = default)
        {
            CreateRequests.Add(request);
            var id = Guid.NewGuid();
            _events.Add(new RestaurantEventListItemResponse
            {
                Id = id,
                Name = request.Name,
                EventDate = request.EventDate,
                EstimatedPizzas = request.EstimatedPizzas,
                EstimatedDoughBalls = request.EstimatedDoughBalls,
                AllowShortFermentation = request.AllowShortFermentation,
                Notes = request.Notes,
                IsActive = request.IsActive,
                UpdatedAtUtc = DateTime.UtcNow
            });

            return Task.FromResult(id);
        }

        public Task UpdateAsync(Guid id, SaveRestaurantEventRequest request, CancellationToken cancellationToken = default)
        {
            UpdateRequests.Add((id, request));
            return Task.CompletedTask;
        }

        public Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
        {
            ActiveStateChanges.Add((id, isActive));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingPrepTaskService : IPrepTaskService
    {
        public List<SavePrepTaskRequest> ManualCreateRequests { get; } = [];

        public List<(Guid PrepTaskId, SavePrepTaskRequest Request)> ManualUpdateRequests { get; } = [];

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
            ManualUpdateRequests.Add((prepTaskId, request));

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
                Message = "Prep task updated successfully."
            });
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
