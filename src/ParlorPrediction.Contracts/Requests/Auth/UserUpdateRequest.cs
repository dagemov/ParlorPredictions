using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Contracts.Requests.Auth;

public sealed class UserUpdateRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = null!;

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public string? ProfileImageUrl { get; set; }
}
