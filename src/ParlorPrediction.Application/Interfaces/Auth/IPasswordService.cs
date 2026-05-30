using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Contracts.Requests.Auth;
using ParlorPrediction.Contracts.Responses.Auth;

namespace ParlorPrediction.Application.Interfaces.Auth;

public interface IPasswordService
{
    Task<ApiResponse<UserResponse>> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<string>> SendPasswordResetEmailAsync(
        ResendEmailRequest request,
        string fallbackBaseUrl,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);
}
