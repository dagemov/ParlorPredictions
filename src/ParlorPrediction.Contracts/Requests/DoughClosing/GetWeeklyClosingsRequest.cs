namespace ParlorPrediction.Contracts.Requests.DoughClosing;

public sealed class GetWeeklyClosingsRequest
{
    public DateOnly? FromWeekStartDate { get; init; }

    public DateOnly? ToWeekStartDate { get; init; }
}
