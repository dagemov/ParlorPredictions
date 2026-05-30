using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Contracts.Requests.Auth;

public sealed class RefreshTokenRequest
{
    [Required]
    public string Token { get; set; } = null!;

    [Required]
    public string RefreshToken { get; set; } = null!;
}
