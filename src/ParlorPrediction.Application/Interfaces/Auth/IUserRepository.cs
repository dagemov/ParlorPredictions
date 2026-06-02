using Microsoft.AspNetCore.Identity;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Interfaces.Auth;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<IdentityResult> CreateAsync(User user, string password);

    Task<IdentityResult> UpdateAsync(User user);

    Task<IReadOnlyList<User>> SearchAsync(
        string? term,
        ApplicationRole? role,
        bool activeOnly,
        bool pendingOnly,
        IReadOnlyCollection<ApplicationRole> allowedRoles,
        CancellationToken cancellationToken = default);

    Task<IdentityResult> AddToRoleAsync(User user, string roleName);

    Task<IdentityResult> RemoveFromRolesAsync(User user, IEnumerable<string> roleNames);

    Task<IdentityResult> EnsureRoleExistsAsync(string roleName);

    Task<IReadOnlyList<string>> GetRoleNamesAsync(User user);

    Task ReloadAsync(User user, CancellationToken cancellationToken = default);

    Task<SignInResult> PasswordSignInAsync(string email, string password);

    Task<string> GenerateEmailConfirmationTokenAsync(User user);

    Task<IdentityResult> ConfirmEmailAsync(User user, string token);

    Task<string> GeneratePasswordResetTokenAsync(User user);

    Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword);

    Task<IdentityResult> ChangePasswordAsync(User user, string currentPassword, string newPassword);

    Task StoreRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task<RefreshToken?> FindRefreshTokenAsync(string token, CancellationToken cancellationToken = default);

    Task UpdateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
}
