using System.Security.Claims;
using ParlorPrediction.Contracts.Responses.Auth;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Auth;

public interface ITokenProvider
{
    TokenResponse BuildToken(User user);

    string GenerateRefreshToken();

    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
