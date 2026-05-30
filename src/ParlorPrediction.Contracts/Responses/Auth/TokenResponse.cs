namespace ParlorPrediction.Contracts.Responses.Auth;

public sealed class TokenResponse
{
    public string Token { get; set; } = null!;

    public DateTime Expiration { get; set; }

    public string RefreshToken { get; set; } = null!;
}
