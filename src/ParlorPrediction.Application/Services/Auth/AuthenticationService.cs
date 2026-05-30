using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Security.Claims;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Contracts.Requests.Auth;
using ParlorPrediction.Contracts.Responses.Auth;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Services.Auth;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly ITokenProvider _tokenProvider;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AuthenticationService(
        ITokenProvider tokenProvider,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _tokenProvider = tokenProvider;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<TokenResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            return ApiResponse<TokenResponse>.Failure("Email or password are incorrect.", HttpStatusCode.BadRequest);
        }

        if (!user.IsActive)
        {
            return ApiResponse<TokenResponse>.Failure("This account is blocked.", HttpStatusCode.Locked);
        }

        var signInResult = await _userRepository.PasswordSignInAsync(request.Email, request.Password);
        if (!signInResult.Succeeded)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (signInResult.IsNotAllowed)
            {
                return ApiResponse<TokenResponse>.Failure(
                    "Confirm your email before signing in.",
                    HttpStatusCode.MethodNotAllowed);
            }

            if (signInResult.IsLockedOut)
            {
                return ApiResponse<TokenResponse>.Failure("This account is temporarily locked.", HttpStatusCode.Locked);
            }

            return ApiResponse<TokenResponse>.Failure("Email or password are incorrect.", HttpStatusCode.BadRequest);
        }

        var tokenResponse = _tokenProvider.BuildToken(user);
        var refreshToken = _tokenProvider.GenerateRefreshToken();

        await _userRepository.StoreRefreshTokenAsync(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            IsUsed = false
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        tokenResponse.RefreshToken = refreshToken;

        return ApiResponse<TokenResponse>.Success(tokenResponse, "Login successful.", HttpStatusCode.OK);
    }

    public async Task<ApiResponse<TokenResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        ClaimsPrincipal principal;

        try
        {
            principal = _tokenProvider.GetPrincipalFromExpiredToken(request.Token);
        }
        catch
        {
            return ApiResponse<TokenResponse>.Failure("Invalid token.", HttpStatusCode.BadRequest);
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ApiResponse<TokenResponse>.Failure("Invalid token.", HttpStatusCode.BadRequest);
        }

        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return ApiResponse<TokenResponse>.Failure("Invalid token.", HttpStatusCode.BadRequest);
        }

        var storedRefreshToken = await _userRepository.FindRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (storedRefreshToken is null ||
            storedRefreshToken.IsRevoked ||
            storedRefreshToken.IsUsed ||
            storedRefreshToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return ApiResponse<TokenResponse>.Failure("Invalid refresh token.", HttpStatusCode.BadRequest);
        }

        storedRefreshToken.IsUsed = true;
        await _userRepository.UpdateRefreshTokenAsync(storedRefreshToken, cancellationToken);

        var newRefreshToken = _tokenProvider.GenerateRefreshToken();
        await _userRepository.StoreRefreshTokenAsync(new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            IsUsed = false
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var tokenResponse = _tokenProvider.BuildToken(user);
        tokenResponse.RefreshToken = newRefreshToken;

        return ApiResponse<TokenResponse>.Success(tokenResponse, "Token refreshed successfully.", HttpStatusCode.OK);
    }
}
