namespace ParlorPrediction.Domain.Entities;

public sealed class RefreshToken
{
    public int Id { get; set; }

    public string Token { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public DateTime ExpiresAtUtc { get; set; }

    public bool IsRevoked { get; set; }

    public bool IsUsed { get; set; }

    public User User { get; set; } = null!;
}
