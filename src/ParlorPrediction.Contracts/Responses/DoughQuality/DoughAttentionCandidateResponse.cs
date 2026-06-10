namespace ParlorPrediction.Contracts.Responses.DoughQuality;

public sealed class DoughAttentionCandidateResponse
{
    public Guid DoughBatchQualityRecordId { get; set; }

    public DateOnly SourceDate { get; set; }

    public DateTime CreatedOrBalledAt { get; set; }

    public int QuantityBalls { get; set; }

    public string CurrentStatus { get; set; } = string.Empty;

    public int AgeDays { get; set; }

    public string CandidateReason { get; set; } = string.Empty;
}
