using Microsoft.AspNetCore.Mvc.Rendering;

namespace ParlorPrediction.Mvc.Models.AdminUsers;

public sealed class AdminUserListPageViewModel
{
    public string? Term { get; init; }

    public string? Role { get; init; }

    public bool ActiveOnly { get; init; } = true;

    public bool PendingOnly { get; init; }

    public IReadOnlyList<SelectListItem> RoleOptions { get; init; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<AdminUserListItemViewModel> Users { get; init; } = Array.Empty<AdminUserListItemViewModel>();

    public string? StatusType { get; init; }

    public string? StatusMessage { get; init; }
}
