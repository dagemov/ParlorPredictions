using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Application.Services.AIOrchestration;
using ParlorPrediction.Application.Services.OperationalDrafts;
using ParlorPrediction.Application.Services.OperationalSimulation;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Mcp.Contracts;
using ParlorPrediction.Mcp.Security;
using ParlorPrediction.Mcp.Tools;
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
        var simulationService = CreateSimulationService();

        var simulation = await simulationService.SimulateAsync(new OperationalNarrativeRequest
        {
            SourceText = "Esta semana sobraron 3 lineas y no quedo carga pendiente. El domingo se hizo una carga y el lunes se boleo.",
            ReferenceDate = new DateOnly(2026, 6, 21),
            TargetWeekStartDate = new DateOnly(2026, 6, 15),
            HistoricalWeeksToUse = 8,
            ActorUserId = "admin-user"
        });

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
        var simulationService = CreateSimulationService();
        var draftService = new OperationalDraftService(simulationService);
        var tools = new WeeklyTools(
            draftService,
            new StubWeeklyDoughClosingReadService(),
            new McpToolAllowlist());

        var result = await tools.DraftWeeklyCorrectionAsync(new DraftWeeklyCorrectionToolRequest
        {
            SourceText = "Esta semana sobraron 3 lineas y no quedo carga pendiente. El domingo se hizo una carga y el lunes se boleo.",
            ReferenceDate = new DateOnly(2026, 6, 21),
            TargetWeekStartDate = new DateOnly(2026, 6, 15),
            HistoricalWeeksToUse = 8,
            ActorUserId = "admin-user"
        });

        Assert.Equal("WeeklyCorrection", result.Draft.DraftType);
        Assert.Equal("admin-user", result.Draft.CreatedByUserId);
        Assert.Equal(result.Draft.Id, result.AuditEntry.DraftId);
        Assert.Contains("504", result.DiffJson, StringComparison.Ordinal);
    }

    private static OperationalSimulationService CreateSimulationService()
    {
        return new OperationalSimulationService(
            new StubDoughAvailabilityProjectionService(),
            new StubDoughInventoryImpactReadService(),
            new OperationalIntentClassifier(),
            new StubPrepWeeklyDoughCalendarService(),
            new StubWeeklyDoughClosingReadService());
    }

    private sealed class StubWeeklyDoughClosingReadService : IWeeklyDoughClosingReadService
    {
        public Task<IReadOnlyList<WeeklyDoughClosingResponse>> GetWeeklyClosingsAsync(
            GetWeeklyClosingsRequest request,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<WeeklyDoughClosingResponse> closings =
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
            ];

            return Task.FromResult(closings);
        }

        public Task<WeeklyDoughCarryoverResponse> GetCarryoverForWeekAsync(
            GetWeeklyDoughCarryoverRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new WeeklyDoughCarryoverResponse
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
            });
        }
    }

    private sealed class StubDoughAvailabilityProjectionService : IDoughAvailabilityProjectionService
    {
        public Task<DoughAvailabilityProjectionResponse> GetWeeklyAvailabilityAsync(
            DateOnly referenceDate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DoughAvailabilityProjectionResponse
            {
                ReferenceDate = referenceDate,
                WeekStartDate = new DateOnly(2026, 6, 16),
                WeekEndDate = new DateOnly(2026, 6, 21),
                HasClosingCarryover = true,
                CarryoverSourceWeekStartDate = new DateOnly(2026, 6, 8),
                CarryoverSourceWeekEndDate = new DateOnly(2026, 6, 14),
                CarryoverReadyBalls = 296,
                CarryoverAvailableBalls = 296,
                CarryoverMixedButNotBalledLoads = 1,
                ProducedThisWeekBalls = 1008,
                ActualUsedBallsThisWeek = 1010,
                LostBallsThisWeek = 0,
                AvailableBalls = 504,
                RegularReadyBalls = 504,
                AttentionAvailableBalls = 0,
                MustUseNextDayBalls = 0
            });
        }
    }

    private sealed class StubPrepWeeklyDoughCalendarService : IPrepWeeklyDoughCalendarService
    {
        public Task<WeeklyDoughCalendarResponse> GetWeekAsync(
            DateOnly referenceDate,
            int historicalWeeksToUse,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new WeeklyDoughCalendarResponse
            {
                WeekStartDate = new DateOnly(2026, 6, 16),
                WeekEndDate = new DateOnly(2026, 6, 21),
                WeekTotalNeededBalls = 943,
                ReadyNowBalls = 504,
                MixedButNotBalledBalls = 0,
                MixedButNotBalledLoads = 0,
                FutureBalls = 0,
                StillMissingThisWeekBalls = 439,
                ProducedThisWeekBalls = 1008
            });
        }
    }

    private sealed class StubDoughInventoryImpactReadService : IDoughInventoryImpactReadService
    {
        public Task<DoughInventoryImpactResponse> GetInventoryImpactAsync(
            ParlorPrediction.Contracts.Requests.Dough.GetDoughInventoryImpactRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DoughInventoryImpactResponse
            {
                ReferenceDate = request.ReferenceDate,
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
            });
        }
    }
}
