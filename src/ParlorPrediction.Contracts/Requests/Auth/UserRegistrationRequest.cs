using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Contracts.Requests.Auth;

public sealed class UserRegistrationRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(250)]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(200, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [Required]
    [StringLength(200, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    public string PasswordConfirmation { get; set; } = null!;

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public string Role { get; set; } = null!;

    public string? ProfileImageUrl { get; set; }
}
