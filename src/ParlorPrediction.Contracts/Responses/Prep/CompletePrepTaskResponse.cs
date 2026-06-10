namespace ParlorPrediction.Contracts.Responses.Prep;

public sealed class CompletePrepTaskResponse
{
    public Guid PrepTaskId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string TaskType { get; set; } = string.Empty;

    public string QuantityUnit { get; set; } = string.Empty;

    public int QuantityCompleted { get; set; }

    public int QuantityCompletedBallsEquivalent { get; set; }

    public DateTime CompletedAtUtc { get; set; }

    public string Message { get; set; } = string.Empty;
}
