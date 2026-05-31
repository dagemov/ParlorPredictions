using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ParlorPredictionDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<User> _signInManager;

    public UserRepository(
        ParlorPredictionDbContext dbContext,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        SignInManager<User> signInManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await _userManager.FindByEmailAsync(email.Trim());
    }

    public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _userManager.FindByIdAsync(userId);
    }

    public Task<IdentityResult> CreateAsync(User user, string password)
    {
        return _userManager.CreateAsync(user, password);
    }

    public Task<IdentityResult> UpdateAsync(User user)
    {
        return _userManager.UpdateAsync(user);
    }

    public async Task<IReadOnlyList<User>> SearchAsync(
        string? term,
        ApplicationRole? role,
        bool activeOnly,
        IReadOnlyCollection<ApplicationRole> allowedRoles,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsNoTracking().AsQueryable();

        if (allowedRoles.Count > 0)
        {
            query = query.Where(user => allowedRoles.Contains(user.Role));
        }

        if (role.HasValue)
        {
            query = query.Where(user => user.Role == role.Value);
        }

        if (activeOnly)
        {
            query = query.Where(user => user.IsActive);
        }

        var normalizedTerm = term?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedTerm))
        {
            query = query.Where(user =>
                (user.FirstName + " " + user.LastName).Contains(normalizedTerm) ||
                (user.Email ?? string.Empty).Contains(normalizedTerm) ||
                (user.UserName ?? string.Empty).Contains(normalizedTerm));
        }

        return await query
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.Email)
            .ToArrayAsync(cancellationToken);
    }

    public Task<IdentityResult> AddToRoleAsync(User user, string roleName)
    {
        return _userManager.AddToRoleAsync(user, ApplicationRoleExtensions.Normalize(roleName));
    }

    public Task<IdentityResult> RemoveFromRolesAsync(User user, IEnumerable<string> roleNames)
    {
        return _userManager.RemoveFromRolesAsync(user, roleNames);
    }

    public async Task<IdentityResult> EnsureRoleExistsAsync(string roleName)
    {
        var normalizedRole = ApplicationRoleExtensions.Normalize(roleName);
        if (string.IsNullOrWhiteSpace(normalizedRole))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Description = "A valid role name is required."
            });
        }

        if (!await _roleManager.RoleExistsAsync(normalizedRole))
        {
            return await _roleManager.CreateAsync(new IdentityRole(normalizedRole));
        }

        return IdentityResult.Success;
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(User user)
    {
        return (await _userManager.GetRolesAsync(user)).ToArray();
    }

    public Task ReloadAsync(User user, CancellationToken cancellationToken = default)
    {
        return _dbContext.Entry(user).ReloadAsync(cancellationToken);
    }

    public async Task<SignInResult> PasswordSignInAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            return SignInResult.Failed;
        }

        if (!user.IsActive)
        {
            return SignInResult.LockedOut;
        }

        return await _signInManager.PasswordSignInAsync(
            user,
            password,
            isPersistent: false,
            lockoutOnFailure: true);
    }

    public Task<string> GenerateEmailConfirmationTokenAsync(User user)
    {
        return _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public Task<IdentityResult> ConfirmEmailAsync(User user, string token)
    {
        return _userManager.ConfirmEmailAsync(user, token);
    }

    public Task<string> GeneratePasswordResetTokenAsync(User user)
    {
        return _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword)
    {
        return _userManager.ResetPasswordAsync(user, token, newPassword);
    }

    public Task<IdentityResult> ChangePasswordAsync(User user, string currentPassword, string newPassword)
    {
        return _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    }

    public async Task StoreRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public Task<RefreshToken?> FindRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens
            .FirstOrDefaultAsync(refreshToken => refreshToken.Token == token, cancellationToken);
    }

    public Task UpdateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _dbContext.RefreshTokens.Update(refreshToken);
        return Task.CompletedTask;
    }
}
