using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ParlorPrediction.Mvc.Models.AdminUsers;

public sealed class AdminUserCreateViewModel
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(250)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    public string PasswordConfirmation { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> RoleOptions { get; set; } = Array.Empty<SelectListItem>();
}
