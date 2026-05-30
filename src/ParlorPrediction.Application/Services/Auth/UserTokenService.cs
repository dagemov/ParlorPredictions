using System.Net;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Services.Auth;

public sealed class UserTokenService : IUserTokenService
{
    private readonly IUserRepository _userRepository;

    public UserTokenService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ApiResponse<string>> GenerateEmailConfirmationTokenAsync(User user)
    {
        var token = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
        return ApiResponse<string>.Success(token, "Confirmation token generated.", HttpStatusCode.OK);
    }

    public async Task<ApiResponse<string>> GeneratePasswordResetTokenAsync(User user)
    {
        var token = await _userRepository.GeneratePasswordResetTokenAsync(user);
        return ApiResponse<string>.Success(token, "Password reset token generated.", HttpStatusCode.OK);
    }
}
