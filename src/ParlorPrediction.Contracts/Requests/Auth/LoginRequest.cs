using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Contracts.Requests.Auth;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(200, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;
}
