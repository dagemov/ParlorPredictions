namespace ParlorPrediction.Contracts.Requests.Prep;

public sealed class SearchPrepTasksRequest
{
    public DateOnly? TaskDate { get; init; }

    public string? Status { get; init; }

    public string? AssignedRole { get; init; }

    public Guid? PrepItemId { get; init; }
}
