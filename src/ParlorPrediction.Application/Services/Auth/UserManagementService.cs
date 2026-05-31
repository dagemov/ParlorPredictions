using System.Net;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Files;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Models.Files;
using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Contracts.Requests.Users;
using ParlorPrediction.Contracts.Responses.Users;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.Auth;

public sealed class UserManagementService : IUserManagementService
{
    private const string ProfilePhotoContainerName = "profile-images";

    private static readonly HashSet<string> AllowedProfileImageExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    ];

    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly IFileBlobService _fileBlobService;
    private readonly IUnitOfWork _unitOfWork;

    public UserManagementService(
        IUserRepository userRepository,
        IEmailConfirmationService emailConfirmationService,
        IFileBlobService fileBlobService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _emailConfirmationService = emailConfirmationService;
        _fileBlobService = fileBlobService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ManagedUserListItemResponse>> SearchAsync(
        SearchUsersRequest request,
        ApplicationRole actingRole,
        CancellationToken cancellationToken = default)
    {
        var allowedRoles = UserManagementRules.GetManageableRoles(actingRole);
        if (allowedRoles.Count == 0)
        {
            return Array.Empty<ManagedUserListItemResponse>();
        }

        var requestedRole = ParseRoleFilter(request.Role, actingRole);
        if (!string.IsNullOrWhiteSpace(request.Role) && requestedRole is null)
        {
            return Array.Empty<ManagedUserListItemResponse>();
        }

        var users = await _userRepository.SearchAsync(
            request.Term,
            requestedRole,
            request.ActiveOnly,
            request.PendingOnly,
            allowedRoles,
            cancellationToken);

        return users.Select(MapListItem).ToArray();
    }

    public async Task<ManagedUserDetailResponse?> GetByIdAsync(
        string userId,
        ApplicationRole actingRole,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null || !UserManagementRules.CanManageRole(actingRole, user.Role))
        {
            return null;
        }

        return MapDetail(user);
    }

    public async Task<ApiResponse<ManagedUserDetailResponse>> CreateAsync(
        CreateManagedUserRequest request,
        ApplicationRole actingRole,
        string fallbackBaseUrl,
        CancellationToken cancellationToken = default)
    {
        if (!ApplicationRoleExtensions.TryParse(request.Role, out var parsedRole))
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure(
                "Choose a valid role before creating the account.",
                HttpStatusCode.BadRequest);
        }

        if (!UserManagementRules.CanAssignRole(actingRole, parsedRole))
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure(
                "You are not allowed to create that role.",
                HttpStatusCode.Forbidden);
        }

        var existingUser = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure(
                "An account with that email already exists.",
                HttpStatusCode.Conflict);
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            Role = parsedRole,
            IsActive = true,
            EmailConfirmed = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var roleResult = await _userRepository.EnsureRoleExistsAsync(parsedRole.GetCanonicalName());
            if (!roleResult.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<ManagedUserDetailResponse>.Failure(
                    "We could not prepare the user role.",
                    HttpStatusCode.BadRequest,
                    roleResult.Errors.Select(static error => error.Description).ToArray());
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var createResult = await _userRepository.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<ManagedUserDetailResponse>.Failure(
                    "We could not create the account.",
                    HttpStatusCode.BadRequest,
                    createResult.Errors.Select(static error => error.Description).ToArray());
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var addToRoleResult = await _userRepository.AddToRoleAsync(user, parsedRole.GetCanonicalName());
            if (!addToRoleResult.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<ManagedUserDetailResponse>.Failure(
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
        var response = MapDetail(user);

        return mailResponse.IsSuccessful
            ? ApiResponse<ManagedUserDetailResponse>.Success(
                response,
                "User created. A confirmation email was sent.",
                HttpStatusCode.Created)
            : ApiResponse<ManagedUserDetailResponse>.Success(
                response,
                "User created, but the confirmation email could not be sent yet.",
                HttpStatusCode.Created);
    }

    public async Task<ApiResponse<ManagedUserDetailResponse>> UpdateAsync(
        string userId,
        UpdateManagedUserRequest request,
        ApplicationRole actingRole,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null || !UserManagementRules.CanManageRole(actingRole, user.Role))
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure("User not found.", HttpStatusCode.NotFound);
        }

        var previousEmail = user.Email ?? string.Empty;
        var nextEmail = request.Email.Trim();

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.UserName = request.UserName.Trim();
        user.Email = nextEmail;
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;

        if (!string.Equals(previousEmail, nextEmail, StringComparison.OrdinalIgnoreCase))
        {
            user.EmailConfirmed = false;
        }

        var result = await _userRepository.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure(
                "We could not update the account.",
                HttpStatusCode.BadRequest,
                result.Errors.Select(static error => error.Description).ToArray());
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var message = string.Equals(previousEmail, nextEmail, StringComparison.OrdinalIgnoreCase)
            ? "User updated successfully."
            : "User updated. The email now needs confirmation again.";

        return ApiResponse<ManagedUserDetailResponse>.Success(
            MapDetail(user),
            message,
            HttpStatusCode.OK);
    }

    public async Task<ApiResponse<ManagedUserDetailResponse>> ChangeRoleAsync(
        string userId,
        ChangeManagedUserRoleRequest request,
        ApplicationRole actingRole,
        string actingUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null || !UserManagementRules.CanManageRole(actingRole, user.Role))
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure("User not found.", HttpStatusCode.NotFound);
        }

        if (string.Equals(user.Id, actingUserId, StringComparison.Ordinal))
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure(
                "Use another admin account before changing your own role.",
                HttpStatusCode.BadRequest);
        }

        if (!ApplicationRoleExtensions.TryParse(request.Role, out var parsedRole))
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure(
                "Choose a valid role before saving.",
                HttpStatusCode.BadRequest);
        }

        if (!UserManagementRules.CanAssignRole(actingRole, parsedRole))
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure(
                "You are not allowed to assign that role.",
                HttpStatusCode.Forbidden);
        }

        if (user.Role == parsedRole)
        {
            return ApiResponse<ManagedUserDetailResponse>.Success(
                MapDetail(user),
                "The user already has that role.",
                HttpStatusCode.OK);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var roleResult = await _userRepository.EnsureRoleExistsAsync(parsedRole.GetCanonicalName());
            if (!roleResult.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<ManagedUserDetailResponse>.Failure(
                    "We could not prepare the requested role.",
                    HttpStatusCode.BadRequest,
                    roleResult.Errors.Select(static error => error.Description).ToArray());
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var roleMembershipChanged = false;

            var currentRoles = await _userRepository.GetRoleNamesAsync(user);
            var removableRoles = currentRoles
                .Where(roleName => ApplicationRoleExtensions.TryParse(roleName, out _))
                .Where(currentRoleName =>
                    !string.Equals(
                        ApplicationRoleExtensions.Normalize(currentRoleName),
                        parsedRole.GetCanonicalName(),
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (removableRoles.Length > 0)
            {
                var removeRolesResult = await _userRepository.RemoveFromRolesAsync(user, removableRoles);
                if (!removeRolesResult.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<ManagedUserDetailResponse>.Failure(
                        "We could not clear the previous role assignment.",
                        HttpStatusCode.BadRequest,
                        removeRolesResult.Errors.Select(static error => error.Description).ToArray());
                }

                roleMembershipChanged = true;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (!currentRoles.Contains(parsedRole.GetCanonicalName(), StringComparer.OrdinalIgnoreCase))
            {
                var addToRoleResult = await _userRepository.AddToRoleAsync(user, parsedRole.GetCanonicalName());
                if (!addToRoleResult.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<ManagedUserDetailResponse>.Failure(
                        "The role could not be assigned.",
                        HttpStatusCode.BadRequest,
                        addToRoleResult.Errors.Select(static error => error.Description).ToArray());
                }

                roleMembershipChanged = true;
            }

            if (roleMembershipChanged)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _userRepository.ReloadAsync(user, cancellationToken);
            }

            user.Role = parsedRole;
            user.UpdatedAtUtc = DateTime.UtcNow;

            var updateResult = await _userRepository.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<ManagedUserDetailResponse>.Failure(
                    "The account was updated, but the role could not be saved on the profile.",
                    HttpStatusCode.BadRequest,
                    updateResult.Errors.Select(static error => error.Description).ToArray());
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return ApiResponse<ManagedUserDetailResponse>.Success(
            MapDetail(user),
            "User role updated successfully.",
            HttpStatusCode.OK);
    }

    public async Task<ApiResponse<ManagedUserDetailResponse>> SetActiveAsync(
        string userId,
        bool isActive,
        ApplicationRole actingRole,
        string actingUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null || !UserManagementRules.CanManageRole(actingRole, user.Role))
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure("User not found.", HttpStatusCode.NotFound);
        }

        if (string.Equals(user.Id, actingUserId, StringComparison.Ordinal) && !isActive)
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure(
                "Use another admin account before deactivating yourself.",
                HttpStatusCode.BadRequest);
        }

        user.IsActive = isActive;
        if (isActive && user.Role == ApplicationRole.Pending)
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure(
                "Assign an operational role before activating this account.",
                HttpStatusCode.BadRequest);
        }

        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await _userRepository.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure(
                "We could not update the user status.",
                HttpStatusCode.BadRequest,
                result.Errors.Select(static error => error.Description).ToArray());
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ManagedUserDetailResponse>.Success(
            MapDetail(user),
            isActive ? "User activated successfully." : "User deactivated successfully.",
            HttpStatusCode.OK);
    }

    public async Task<ApiResponse<string>> ResendConfirmationAsync(
        string userId,
        ApplicationRole actingRole,
        string fallbackBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null || !UserManagementRules.CanManageRole(actingRole, user.Role))
        {
            return ApiResponse<string>.Failure("User not found.", HttpStatusCode.NotFound);
        }

        if (user.EmailConfirmed)
        {
            return ApiResponse<string>.Success(
                user.Email,
                "That account is already confirmed.",
                HttpStatusCode.OK);
        }

        return await _emailConfirmationService.SendConfirmationEmailAsync(user, fallbackBaseUrl, cancellationToken);
    }

    public async Task<ApiResponse<ManagedUserDetailResponse>> UpdateProfilePhotoAsync(
        string userId,
        byte[] content,
        string extension,
        string originalFileName,
        string contentType,
        ApplicationRole actingRole,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null || !UserManagementRules.CanManageRole(actingRole, user.Role))
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure("User not found.", HttpStatusCode.NotFound);
        }

        var normalizedExtension = NormalizeExtension(extension);
        if (!AllowedProfileImageExtensions.Contains(normalizedExtension))
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure(
                "Choose a JPG, PNG, or WEBP image for the profile photo.",
                HttpStatusCode.BadRequest);
        }

        if (content.Length == 0 || content.Length > 3 * 1024 * 1024)
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure(
                "Profile photos must be between 1 byte and 3 MB.",
                HttpStatusCode.BadRequest);
        }

        var payload = new FileUploadPayload
        {
            Content = content,
            Extension = normalizedExtension,
            OriginalFileName = originalFileName,
            ContentType = contentType,
            ContainerName = ProfilePhotoContainerName
        };

        ApiResponse<Contracts.Responses.Files.FileUploadResponse> uploadResponse;
        if (string.IsNullOrWhiteSpace(user.ProfileImageUrl))
        {
            uploadResponse = await _fileBlobService.UploadFileAsync(payload, cancellationToken);
        }
        else
        {
            uploadResponse = await _fileBlobService.ReplaceFileAsync(
                payload,
                user.ProfileImageUrl,
                ProfilePhotoContainerName,
                cancellationToken);
        }

        if (!uploadResponse.IsSuccessful || uploadResponse.Result is null)
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure(
                uploadResponse.Message,
                uploadResponse.StatusCode,
                uploadResponse.Errors.ToArray());
        }

        user.ProfileImageUrl = uploadResponse.Result.Url;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await _userRepository.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return ApiResponse<ManagedUserDetailResponse>.Failure(
                "The photo uploaded, but the user record could not be updated.",
                HttpStatusCode.BadRequest,
                result.Errors.Select(static error => error.Description).ToArray());
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ManagedUserDetailResponse>.Success(
            MapDetail(user),
            "Profile photo updated successfully.",
            HttpStatusCode.OK);
    }

    private static ApplicationRole? ParseRoleFilter(string? role, ApplicationRole actingRole)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        if (!ApplicationRoleExtensions.TryParse(role, out var parsedRole))
        {
            return null;
        }

        return UserManagementRules.CanManageRole(actingRole, parsedRole)
            ? parsedRole
            : null;
    }

    private static ManagedUserListItemResponse MapListItem(User user)
    {
        return new ManagedUserListItemResponse
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.GetCanonicalName(),
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            ProfileImageUrl = user.ProfileImageUrl,
            UpdatedAtUtc = user.UpdatedAtUtc
        };
    }

    private static ManagedUserDetailResponse MapDetail(User user)
    {
        return new ManagedUserDetailResponse
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.GetCanonicalName(),
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            ProfileImageUrl = user.ProfileImageUrl,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc
        };
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        var normalized = extension.Trim().ToLowerInvariant();
        return normalized.StartsWith('.') ? normalized : $".{normalized}";
    }
}
