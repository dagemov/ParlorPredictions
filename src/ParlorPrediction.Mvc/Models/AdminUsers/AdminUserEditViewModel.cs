using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Mvc.Models.AdminUsers;

public sealed class AdminUserEditViewModel
{
    public string Id { get; set; } = string.Empty;

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

    public string? CurrentProfileImageUrl { get; set; }
}
