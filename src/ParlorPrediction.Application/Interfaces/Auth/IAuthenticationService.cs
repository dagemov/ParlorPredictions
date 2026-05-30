using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Contracts.Requests.Auth;
using ParlorPrediction.Contracts.Responses.Auth;

namespace ParlorPrediction.Application.Interfaces.Auth;

public interface IAuthenticationService
{
    Task<ApiResponse<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
}
