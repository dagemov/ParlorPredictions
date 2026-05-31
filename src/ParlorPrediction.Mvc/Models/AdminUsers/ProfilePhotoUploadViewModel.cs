namespace ParlorPrediction.Mvc.Models.AdminUsers;

public sealed class ProfilePhotoUploadViewModel
{
    public string UserId { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string? CurrentPhotoUrl { get; init; }
}
