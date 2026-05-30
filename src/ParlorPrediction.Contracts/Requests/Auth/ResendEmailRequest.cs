using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Contracts.Requests.Auth;

public sealed class ResendEmailRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}
