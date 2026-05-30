using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Contracts.Requests.Auth;
using ParlorPrediction.Contracts.Responses.Auth;

namespace ParlorPrediction.Application.Interfaces.Auth;

public interface IAccountService
{
    Task<ApiResponse<UserResponse>> RegisterAsync(
        UserRegistrationRequest request,
        string fallbackBaseUrl,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<UserResponse>> UpdateAsync(
        string userId,
        UserUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<UserResponse>> GetCurrentUserAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
