using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Auth;

public interface IUserTokenService
{
    Task<ApiResponse<string>> GenerateEmailConfirmationTokenAsync(User user);

    Task<ApiResponse<string>> GeneratePasswordResetTokenAsync(User user);
}
