namespace ParlorPrediction.Contracts.Responses.Prep;

public sealed class CompletePrepTaskResponse
{
    public Guid PrepTaskId { get; set; }

    public string Status { get; set; } = string.Empty;

    public int QuantityCompleted { get; set; }

    public DateTime CompletedAtUtc { get; set; }

    public string Message { get; set; } = string.Empty;
}
