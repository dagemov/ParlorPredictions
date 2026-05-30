using Microsoft.AspNetCore.Identity;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Domain.Entities;

public sealed class User : IdentityUser
{
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? ProfileImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public ApplicationRole Role { get; set; } = ApplicationRole.Manager;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
