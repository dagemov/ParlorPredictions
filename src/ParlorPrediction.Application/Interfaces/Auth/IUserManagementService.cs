using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Contracts.Requests.Users;
using ParlorPrediction.Contracts.Responses.Users;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Interfaces.Auth;

public interface IUserManagementService
{
    Task<IReadOnlyList<ManagedUserListItemResponse>> SearchAsync(
        SearchUsersRequest request,
        ApplicationRole actingRole,
        CancellationToken cancellationToken = default);

    Task<ManagedUserDetailResponse?> GetByIdAsync(
        string userId,
        ApplicationRole actingRole,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ManagedUserDetailResponse>> CreateAsync(
        CreateManagedUserRequest request,
        ApplicationRole actingRole,
        string fallbackBaseUrl,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ManagedUserDetailResponse>> UpdateAsync(
        string userId,
        UpdateManagedUserRequest request,
        ApplicationRole actingRole,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ManagedUserDetailResponse>> ChangeRoleAsync(
        string userId,
        ChangeManagedUserRoleRequest request,
        ApplicationRole actingRole,
        string actingUserId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ManagedUserDetailResponse>> SetActiveAsync(
        string userId,
        bool isActive,
        ApplicationRole actingRole,
        string actingUserId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<string>> ResendConfirmationAsync(
        string userId,
        ApplicationRole actingRole,
        string fallbackBaseUrl,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ManagedUserDetailResponse>> UpdateProfilePhotoAsync(
        string userId,
        byte[] content,
        string extension,
        string originalFileName,
        string contentType,
        ApplicationRole actingRole,
        CancellationToken cancellationToken = default);
}
