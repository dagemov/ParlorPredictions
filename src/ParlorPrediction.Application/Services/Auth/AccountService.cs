using Microsoft.AspNetCore.Identity;
using System.Net;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Mappings;
using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Contracts.Requests.Auth;
using ParlorPrediction.Contracts.Responses.Auth;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Services.Auth;

public sealed class AccountService : IAccountService
{
    private const string PublicRegistrationMessage =
        "Check your email to confirm your account. After you confirm it, an admin still needs to approve your access.";

    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(
        IUserRepository userRepository,
        IEmailConfirmationService emailConfirmationService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _emailConfirmationService = emailConfirmationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<UserResponse>> RegisterAsync(
        UserRegistrationRequest request,
        string fallbackBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            if (!existingUser.EmailConfirmed)
            {
                await _emailConfirmationService.SendConfirmationEmailAsync(existingUser, fallbackBaseUrl, cancellationToken);
            }

            return ApiResponse<UserResponse>.Success(
                null,
                PublicRegistrationMessage,
                HttpStatusCode.Accepted);
        }

        var parsedRole = ApplicationRole.Pending;
        var user = request.ToUser(parsedRole);
        user.IsActive = false;
        user.EmailConfirmed = false;
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var roleResult = await _userRepository.EnsureRoleExistsAsync(parsedRole.GetCanonicalName());
            if (!roleResult.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<UserResponse>.Failure(
                    "We could not prepare the user role.",
                    HttpStatusCode.BadRequest,
                    roleResult.Errors.Select(static error => error.Description).ToArray());
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var createResult = await _userRepository.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<UserResponse>.Failure(
                    "We could not create the account.",
                    HttpStatusCode.BadRequest,
                    createResult.Errors.Select(static error => error.Description).ToArray());
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var addToRoleResult = await _userRepository.AddToRoleAsync(user, parsedRole.GetCanonicalName());
            if (!addToRoleResult.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<UserResponse>.Failure(
                    "The account was created, but the role assignment failed.",
                    HttpStatusCode.BadRequest,
                    addToRoleResult.Errors.Select(static error => error.Description).ToArray());
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var mailResponse = await _emailConfirmationService.SendConfirmationEmailAsync(user, fallbackBaseUrl, cancellationToken);
        var response = user.ToUserResponse();

        return mailResponse.IsSuccessful
            ? ApiResponse<UserResponse>.Success(response, PublicRegistrationMessage, HttpStatusCode.Created)
            : ApiResponse<UserResponse>.Success(response, "Your registration was saved, but the confirmation email could not be sent yet.", HttpStatusCode.Created);
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
