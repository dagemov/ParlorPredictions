namespace ParlorPrediction.Contracts.Responses.Users;

public sealed class ManagedUserListItemResponse
{
    public string Id { get; init; } = string.Empty;

    public string UserName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public string Role { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public bool EmailConfirmed { get; init; }

    public string? ProfileImageUrl { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}
