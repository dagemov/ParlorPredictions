using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Mvc.Models.DoughClosing;

public sealed class WeeklyDoughClosingListItemViewModel
{
    public Guid Id { get; set; }

    public DateOnly WeekStartDate { get; set; }

    public DateOnly WeekEndDate { get; set; }

    public int NeededBalls { get; set; }

    public int ProducedBalls { get; set; }

    public int UsedBalls { get; set; }

    public int LostBalls { get; set; }

    public int LeftoverReadyBalls { get; set; }

    public int LeftoverAttentionBalls { get; set; }

    public int LeftoverMixedLoads { get; set; }

    public int CarryoverAvailableBalls { get; set; }

    public string? Notes { get; set; }

    public string ClosedByUserId { get; set; } = string.Empty;

    public DateTime ClosedAtUtc { get; set; }

    public bool WasCorrected { get; set; }

    public string? CorrectedByUserId { get; set; }

    public DateTime? CorrectedAtUtc { get; set; }

    public string? CorrectionNote { get; set; }

    public int LeftoverMixedPotentialBalls => LeftoverMixedLoads * DoughRules.StandardBatchBalls;

    public int NeededLoads => ToLoads(NeededBalls);

    public int ProducedLoads => ToLoads(ProducedBalls);

    public int UsedLoads => ToLoads(UsedBalls);

    public int LostLoads => ToLoads(LostBalls);

    public int CarryoverAvailableLoads => ToLoads(CarryoverAvailableBalls);

    private static int ToLoads(int balls)
    {
        return balls <= 0
            ? 0
            : (int)Math.Ceiling(balls / (double)DoughRules.StandardBatchBalls);
    }
}
