using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Mvc.Models.DoughClosing;

public sealed class WeeklyDoughCarryoverPreviewViewModel
{
    public DateOnly ReferenceDate { get; set; }

    public DateOnly TargetWeekStartDate { get; set; }

    public DateOnly TargetWeekEndDate { get; set; }

    public bool HasClosingCarryover { get; set; }

    public DateOnly? SourceWeekStartDate { get; set; }

    public DateOnly? SourceWeekEndDate { get; set; }

    public int CarryoverReadyBalls { get; set; }

    public int CarryoverAttentionBalls { get; set; }

    public int CarryoverAvailableBalls { get; set; }

    public int MixedButNotBalledLoads { get; set; }

    public int MixedButNotBalledPotentialBalls { get; set; }

    public int PreviousWeekProducedBalls { get; set; }

    public int PreviousWeekUsedBalls { get; set; }

    public int PreviousWeekLostBalls { get; set; }

    public string? ClosingNotes { get; set; }

    public int CarryoverAvailableLoads => ToLoads(CarryoverAvailableBalls);

    public int MixedButNotBalledPotentialLoads => ToLoads(MixedButNotBalledPotentialBalls);

    public int PreviousWeekProducedLoads => ToLoads(PreviousWeekProducedBalls);

    public int PreviousWeekUsedLoads => ToLoads(PreviousWeekUsedBalls);

    public int PreviousWeekLostLoads => ToLoads(PreviousWeekLostBalls);

    private static int ToLoads(int balls)
    {
        return balls <= 0
            ? 0
            : (int)Math.Ceiling(balls / (double)DoughRules.StandardBatchBalls);
    }
}
