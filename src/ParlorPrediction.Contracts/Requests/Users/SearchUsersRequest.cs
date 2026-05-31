namespace ParlorPrediction.Contracts.Requests.Users;

public sealed class SearchUsersRequest
{
    public string? Term { get; init; }

    public string? Role { get; init; }

    public bool ActiveOnly { get; init; } = true;
}
