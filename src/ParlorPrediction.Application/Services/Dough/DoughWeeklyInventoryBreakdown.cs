using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.Dough;

public sealed record DoughWeeklyInventoryBreakdown(
    int ReadyNowBalls,
    int MixedButNotBalledBalls,
    int MixedButNotBalledLoads,
    int StillFermentingBalls,
    int FutureBalls);

public static class DoughWeeklyInventoryCalculator
{
    public static DoughWeeklyInventoryBreakdown Calculate(
        DateOnly referenceDate,
        DateOnly weekEndDate,
        int readyNowBalls,
        IReadOnlyCollection<DoughBatch> doughBatches,
        int carryoverMixedLoads = 0,
        bool applyCarryoverMixedFallback = false)
    {
        var liveMixedButNotBalledBalls = doughBatches
            .Where(batch => !batch.IsBalled)
            .Sum(batch => batch.TotalBalls);
        var carryoverMixedButNotBalledBalls = applyCarryoverMixedFallback && liveMixedButNotBalledBalls == 0
            ? carryoverMixedLoads * DoughRules.StandardBatchBalls
            : 0;
        var mixedButNotBalledBalls = liveMixedButNotBalledBalls + carryoverMixedButNotBalledBalls;

        var stillFermentingBalls = doughBatches
            .Where(batch =>
                batch.IsBalled &&
                batch.FermentationReadyDate > referenceDate &&
                batch.FermentationReadyDate <= weekEndDate)
            .Sum(batch => batch.TotalBalls);

        var mixedButNotBalledLoads = CountFullLoads(mixedButNotBalledBalls);

        var futureBalls = mixedButNotBalledBalls + stillFermentingBalls;

        return new DoughWeeklyInventoryBreakdown(
            readyNowBalls,
            mixedButNotBalledBalls,
            mixedButNotBalledLoads,
            stillFermentingBalls,
            futureBalls);
    }

    public static int CalculateStillMissingThisWeek(
        int weekTotalNeededBalls,
        DoughWeeklyInventoryBreakdown inventory,
        int actualUsedBallsThisWeek = 0)
    {
        var remainingNeed = Math.Max(weekTotalNeededBalls - actualUsedBallsThisWeek, 0);

        // Still Missing should reflect what is not covered by dough that physically exists right now.
        // Future dough is shown separately for planning, but it must not reduce the current shortage.
        return Math.Max(
            remainingNeed
                - inventory.ReadyNowBalls,
            0);
    }

    public static int ResolveCarryoverAnchoredReadyBalls(
        int snapshotReadyBalls,
        int carryoverAvailableBalls,
        bool hasClosingCarryover,
        bool hasCurrentWeekSnapshot,
        int producedThisWeekBalls,
        int actualUsedBallsThisWeek)
    {
        if (!hasClosingCarryover)
        {
            return snapshotReadyBalls;
        }

        return Math.Max(
            0,
            carryoverAvailableBalls + producedThisWeekBalls - actualUsedBallsThisWeek);
    }

    public static int ResolveStillFermentingForDisplay(
        int stillFermentingBalls,
        int producedThisWeekBalls)
    {
        if (producedThisWeekBalls <= 0)
        {
            return stillFermentingBalls;
        }

        // Ball Dough completion moves balls into Ready Now; do not duplicate them on the fermenting card.
        return Math.Max(0, stillFermentingBalls - producedThisWeekBalls);
    }

    public static bool UsesLiveInventorySnapshot(DoughInventorySnapshot? latestInventorySnapshot, DateOnly weekStartDate)
    {
        return latestInventorySnapshot is not null && latestInventorySnapshot.SnapshotDate >= weekStartDate;
    }

    private static int CountFullLoads(int balls)
    {
        return balls <= 0
            ? 0
            : (int)Math.Ceiling(balls / (double)DoughRules.StandardBatchBalls);
    }
}
