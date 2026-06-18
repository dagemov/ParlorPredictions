namespace ParlorPrediction.Contracts.Responses.Dough;

public sealed class DoughInventoryImpactSourceResponse
{
    public Guid SourceDoughBatchQualityRecordId { get; set; }

    public DateOnly SourceDate { get; set; }

    public DateTime CreatedOrBalledAt { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public DateOnly? MustUseByDate { get; set; }

    public int AgeDays { get; set; }

    public int OriginalBalls { get; set; }

    public int UsedBalls { get; set; }

    public int RemainingBalls { get; set; }

    public bool CountsAsAvailable { get; set; }

    public bool IsReballCandidate { get; set; }

    public bool IsDiscardCandidate { get; set; }

    public string RecommendedAction { get; set; } = string.Empty;
}
