using ParlorPrediction.Application.Services.Dough;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Rules;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class DoughWeeklyInventoryCalculatorTests
{
    [Fact]
    public void UnballedLoads_AreMixedNotFermenting()
    {
        var referenceDate = new DateOnly(2026, 6, 9);
        var weekEndDate = referenceDate.AddDays(5);
        var batches = new[]
        {
            new DoughBatch(Guid.NewGuid(), referenceDate, DoughBatch.StandardLoadCases)
        };

        var breakdown = DoughWeeklyInventoryCalculator.Calculate(
            referenceDate,
            weekEndDate,
            readyNowBalls: 432,
            batches);

        Assert.Equal(432, breakdown.ReadyNowBalls);
        Assert.Equal(168, breakdown.MixedButNotBalledBalls);
        Assert.Equal(1, breakdown.MixedButNotBalledLoads);
        Assert.Equal(0, breakdown.StillFermentingBalls);
        Assert.Equal(168, breakdown.FutureBalls);
    }

    [Fact]
    public void StillMissing_UsesReadyNowOnly_AndLeavesFutureDoughSeparate()
    {
        var breakdown = new DoughWeeklyInventoryBreakdown(
            ReadyNowBalls: 432,
            MixedButNotBalledBalls: 168,
            MixedButNotBalledLoads: 1,
            StillFermentingBalls: 0,
            FutureBalls: 168);

        var stillMissing = DoughWeeklyInventoryCalculator.CalculateStillMissingThisWeek(600, breakdown);

        Assert.Equal(168, stillMissing);
    }

    [Fact]
    public void StillMissing_DoesNotSubtractFermentingTwiceAfterBallCompletion()
    {
        var breakdown = new DoughWeeklyInventoryBreakdown(
            ReadyNowBalls: 600,
            MixedButNotBalledBalls: 0,
            MixedButNotBalledLoads: 0,
            StillFermentingBalls: 168,
            FutureBalls: 168);

        var stillMissing = DoughWeeklyInventoryCalculator.CalculateStillMissingThisWeek(1063, breakdown);

        Assert.Equal(463, stillMissing);
    }

    [Fact]
    public void StillMissing_CreditsActualUsedFromDailyClosings()
    {
        var breakdown = new DoughWeeklyInventoryBreakdown(
            ReadyNowBalls: 432,
            MixedButNotBalledBalls: 168,
            MixedButNotBalledLoads: 1,
            StillFermentingBalls: 0,
            FutureBalls: 168);

        var stillMissing = DoughWeeklyInventoryCalculator.CalculateStillMissingThisWeek(1063, breakdown, actualUsedBallsThisWeek: 45);

        Assert.Equal(586, stillMissing);
    }

    [Fact]
    public void CarryoverFallback_DoesNotDuplicateLivePendingLoad()
    {
        var referenceDate = new DateOnly(2026, 6, 17);
        var weekEndDate = referenceDate.AddDays(4);
        var batches = new[]
        {
            new DoughBatch(Guid.NewGuid(), referenceDate, DoughBatch.StandardLoadCases)
        };

        var breakdown = DoughWeeklyInventoryCalculator.Calculate(
            referenceDate,
            weekEndDate,
            readyNowBalls: 720,
            batches,
            carryoverMixedLoads: 1,
            applyCarryoverMixedFallback: true);

        Assert.Equal(168, breakdown.MixedButNotBalledBalls);
        Assert.Equal(1, breakdown.MixedButNotBalledLoads);
        Assert.Equal(168, breakdown.FutureBalls);
    }

    [Fact]
    public void StillMissing_Equals223_WhenWeeklyRemainingNeedIs943_AndReadyNowIs720()
    {
        var breakdown = new DoughWeeklyInventoryBreakdown(
            ReadyNowBalls: 720,
            MixedButNotBalledBalls: 168,
            MixedButNotBalledLoads: 1,
            StillFermentingBalls: 0,
            FutureBalls: 168);

        var stillMissing = DoughWeeklyInventoryCalculator.CalculateStillMissingThisWeek(943, breakdown);

        Assert.Equal(223, stillMissing);
    }

    [Fact]
    public void CarryoverAnchoredReady_IgnoresLowSnapshotWhenCarryoverExists()
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
    public void CarryoverAnchoredReady_UsesClosingBaselinePlusProductionMinusConsumption()
    {
        var ready = DoughWeeklyInventoryCalculator.ResolveCarryoverAnchoredReadyBalls(
            snapshotReadyBalls: 192,
            carryoverAvailableBalls: 432,
            hasClosingCarryover: true,
            hasCurrentWeekSnapshot: true,
            producedThisWeekBalls: 168,
            actualUsedBallsThisWeek: 45);

        Assert.Equal(555, ready);
    }

    [Fact]
    public void StillFermentingForDisplay_HidesBallsAlreadyMovedIntoReady()
    {
        Assert.Equal(0, DoughWeeklyInventoryCalculator.ResolveStillFermentingForDisplay(168, 168));
        Assert.Equal(84, DoughWeeklyInventoryCalculator.ResolveStillFermentingForDisplay(168, 84));
    }

    [Fact]
    public void TwoFullLoadsPlusEightCases_Equals432Ready()
    {
        var readyBalls = (2 * DoughRules.StandardBatchBalls) + (8 * DoughRules.BallsPerCase);
        Assert.Equal(432, readyBalls);
    }
}
