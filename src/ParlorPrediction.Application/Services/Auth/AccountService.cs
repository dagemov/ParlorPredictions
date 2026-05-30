using Microsoft.AspNetCore.Identity;
using System.Net;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Mappings;
using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Contracts.Requests.Auth;
using ParlorPrediction.Contracts.Responses.Auth;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Services.Auth;

public sealed class AccountService : IAccountService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationService _emailConfirmationService;

    public AccountService(
        IUserRepository userRepository,
        IEmailConfirmationService emailConfirmationService)
    {
        _userRepository = userRepository;
        _emailConfirmationService = emailConfirmationService;
    }

    public async Task<ApiResponse<UserResponse>> RegisterAsync(
        UserRegistrationRequest request,
        string fallbackBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            return ApiResponse<UserResponse>.Failure(
                "An account with that email already exists.",
                HttpStatusCode.Conflict);
        }

        if (!ApplicationRoleExtensions.TryParse(request.Role, out var parsedRole))
        {
            return ApiResponse<UserResponse>.Failure(
                "Choose a valid Parlor role before creating the account.",
                HttpStatusCode.BadRequest);
        }

        var user = request.ToUser(parsedRole);

        await _userRepository.EnsureRoleExistsAsync(parsedRole.GetCanonicalName());

        var createResult = await _userRepository.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return ApiResponse<UserResponse>.Failure(
                "We could not create the account.",
                HttpStatusCode.BadRequest,
                createResult.Errors.Select(static error => error.Description).ToArray());
        }

        var roleResult = await _userRepository.AddToRoleAsync(user, parsedRole.GetCanonicalName());
        if (!roleResult.Succeeded)
        {
            return ApiResponse<UserResponse>.Failure(
                "The account was created, but the role assignment failed.",
                HttpStatusCode.BadRequest,
                roleResult.Errors.Select(static error => error.Description).ToArray());
        }

        var mailResponse = await _emailConfirmationService.SendConfirmationEmailAsync(user, fallbackBaseUrl, cancellationToken);
        var response = user.ToUserResponse();

        return mailResponse.IsSuccessful
            ? ApiResponse<UserResponse>.Success(response, "Account created. Check email to confirm it before signing in.", HttpStatusCode.Created)
            : ApiResponse<UserResponse>.Success(response, "Account created, but confirmation email could not be sent yet.", HttpStatusCode.Created);
    }

    public async Task<ApiResponse<UserResponse>> UpdateAsync(
        string userId,
        UserUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ApiResponse<UserResponse>.Failure("User not found.", HttpStatusCode.NotFound);
        }

        request.Apply(user);
        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await _userRepository.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return ApiResponse<UserResponse>.Failure(
                "We could not update the account.",
                HttpStatusCode.BadRequest,
                result.Errors.Select(static error => error.Description).ToArray());
        }

        return ApiResponse<UserResponse>.Success(
            user.ToUserResponse(),
            "Account updated successfully.",
            HttpStatusCode.OK);
    }

    public async Task<ApiResponse<UserResponse>> GetCurrentUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ApiResponse<UserResponse>.Failure("User not found.", HttpStatusCode.NotFound);
        }

        return ApiResponse<UserResponse>.Success(
            user.ToUserResponse(),
            "Authenticated user loaded.",
            HttpStatusCode.OK);
    }
}
