namespace ParlorPrediction.Contracts.Requests.Dough;

public sealed class AdminCorrectDoughBatchRequest
{
    public Guid DoughBatchId { get; init; }

    public DateOnly BatchDate { get; init; }

    public int TotalCases { get; init; }

    public bool IsBalled { get; init; }

    public DateTime? BalledAtUtc { get; init; }

    public bool IsEventException { get; init; }

    public bool IsVoided { get; init; }

    public string? VoidReason { get; init; }

    public string? Notes { get; init; }

    public string UpdatedByUserId { get; init; } = string.Empty;
}
