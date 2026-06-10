namespace ParlorPrediction.Contracts.Requests.DoughClosing;

public sealed class CorrectWeeklyDoughClosingRequest
{
    public Guid WeeklyDoughClosingId { get; init; }

    public int NeededBalls { get; init; }

    public int ProducedBalls { get; init; }

    public int UsedBalls { get; init; }

    public int LostBalls { get; init; }

    public int LeftoverReadyBalls { get; init; }

    public int LeftoverAttentionBalls { get; init; }

    public int LeftoverMixedLoads { get; init; }

    public string? Notes { get; init; }

    public string CorrectedByUserId { get; init; } = string.Empty;

    public DateTime? CorrectedAtUtc { get; init; }

    public string? CorrectionNote { get; init; }
}
