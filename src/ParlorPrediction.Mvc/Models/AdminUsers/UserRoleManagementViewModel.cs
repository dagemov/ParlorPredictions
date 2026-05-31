using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ParlorPrediction.Mvc.Models.AdminUsers;

public sealed class UserRoleManagementViewModel
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string CurrentRole { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool EmailConfirmed { get; set; }

    public string? ProfileImageUrl { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> RoleOptions { get; set; } = Array.Empty<SelectListItem>();
}
