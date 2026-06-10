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

        var mixedButNotBalledLoads = liveMixedButNotBalledBalls > 0
            ? CountFullLoads(liveMixedButNotBalledBalls) + (applyCarryoverMixedFallback ? carryoverMixedLoads : 0)
            : applyCarryoverMixedFallback
                ? carryoverMixedLoads
                : 0;

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

        // ReadyNow already includes balled dough added on Ball Dough completion.
        // MixedButNotBalled covers unballed loads. StillFermenting is shown separately for kitchen visibility
        // and must not be subtracted again here or completed loads double-reduce Still Missing.
        return Math.Max(
            remainingNeed
                - inventory.ReadyNowBalls
                - inventory.MixedButNotBalledBalls,
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

        var transactionReady = Math.Max(
            0,
            carryoverAvailableBalls + producedThisWeekBalls - actualUsedBallsThisWeek);

        if (!hasCurrentWeekSnapshot)
        {
            return transactionReady;
        }

        // Priority: Weekly Closing carryover + current-week transactions over a low runtime snapshot.
        // A higher snapshot still wins when inventory was explicitly corrected upward.
        return Math.Max(transactionReady, snapshotReadyBalls);
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
