using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParlorPrediction.Application.Configuration;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Persistence.Services;

public sealed class AuthBootstrapper
{
    private static readonly HashSet<string> BuiltInRoleNames = ApplicationRoleExtensions
        .GetAllNames()
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private readonly DevelopmentSeedUsersOptions _developmentSeedUsersOptions;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<AuthBootstrapper> _logger;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly BootstrapAdminOptions _bootstrapAdminOptions;
    private readonly IUnitOfWork _unitOfWork;

    public AuthBootstrapper(
        IHostEnvironment hostEnvironment,
        ILogger<AuthBootstrapper> logger,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<BootstrapAdminOptions> bootstrapAdminOptions,
        IOptions<DevelopmentSeedUsersOptions> developmentSeedUsersOptions,
        IUnitOfWork unitOfWork)
    {
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _userManager = userManager;
        _roleManager = roleManager;
        _bootstrapAdminOptions = bootstrapAdminOptions.Value;
        _developmentSeedUsersOptions = developmentSeedUsersOptions.Value;
        _unitOfWork = unitOfWork;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!await EnsureBuiltInRolesAsync(cancellationToken))
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return;
            }

            if (!await EnsureBootstrapManagerAsync(cancellationToken))
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return;
            }

            if (!await EnsureDevelopmentUsersAsync(cancellationToken))
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return;
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task<bool> EnsureBuiltInRolesAsync(CancellationToken cancellationToken)
    {
        var createdAnyRole = false;
        foreach (var builtInRoleName in ApplicationRoleExtensions.GetAllNames())
        {
            if (await _roleManager.RoleExistsAsync(builtInRoleName))
            {
                continue;
            }

            var roleCreationResult = await _roleManager.CreateAsync(new IdentityRole(builtInRoleName));
            if (!roleCreationResult.Succeeded)
            {
                _logger.LogWarning(
                    "Bootstrap role {RoleName} could not be created: {Errors}",
                    builtInRoleName,
                    string.Join("; ", roleCreationResult.Errors.Select(static error => error.Description)));
                return false;
            }

            createdAnyRole = true;
        }

        if (createdAnyRole)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private async Task<bool> EnsureBootstrapManagerAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Email))
        {
            return true;
        }

        var existingUser = await _userManager.FindByEmailAsync(_bootstrapAdminOptions.Email);
        if (existingUser is not null)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Password))
        {
            _logger.LogWarning("Bootstrap admin password is not configured. Manager seed was skipped.");
            return true;
        }

        var parsedRole = ApplicationRoleExtensions.TryParse(_bootstrapAdminOptions.Role, out var configuredRole)
            ? configuredRole
            : ApplicationRole.Manager;
        var roleName = parsedRole.GetCanonicalName();

        var user = new User
        {
            FirstName = _bootstrapAdminOptions.FirstName,
            LastName = _bootstrapAdminOptions.LastName,
            UserName = _bootstrapAdminOptions.UserName,
            Email = _bootstrapAdminOptions.Email,
            PhoneNumber = _bootstrapAdminOptions.PhoneNumber,
            Role = parsedRole,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, _bootstrapAdminOptions.Password);
        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "Bootstrap admin could not be created: {Errors}",
                string.Join("; ", result.Errors.Select(static error => error.Description)));
            return false;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var addToRoleResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!addToRoleResult.Succeeded)
        {
            _logger.LogWarning(
                "Bootstrap admin role assignment failed: {Errors}",
                string.Join("; ", addToRoleResult.Errors.Select(static error => error.Description)));
            return false;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> EnsureDevelopmentUsersAsync(CancellationToken cancellationToken)
    {
        if (!_hostEnvironment.IsDevelopment() || _developmentSeedUsersOptions.Users.Count == 0)
        {
            return true;
        }

        foreach (var developmentUser in _developmentSeedUsersOptions.Users)
        {
            var ensured = await EnsureDevelopmentUserAsync(developmentUser, cancellationToken);
            if (!ensured)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> EnsureDevelopmentUserAsync(
        DevelopmentSeedUserOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Email) ||
            string.IsNullOrWhiteSpace(options.Password) ||
            string.IsNullOrWhiteSpace(options.UserName))
        {
            _logger.LogWarning("A development seed user is missing required values. The entry was skipped.");
            return false;
        }

        if (!ApplicationRoleExtensions.TryParse(options.Role, out var parsedRole))
        {
            _logger.LogWarning(
                "Development seed user {Email} has an invalid role value {Role}.",
                options.Email,
                options.Role);
            return false;
        }

        var desiredRoleName = parsedRole.GetCanonicalName();
        var user = await _userManager.FindByEmailAsync(options.Email);

        if (user is null)
        {
            user = new User
            {
                FirstName = options.FirstName,
                LastName = options.LastName,
                UserName = options.UserName,
                Email = options.Email,
                PhoneNumber = options.PhoneNumber,
                Role = parsedRole,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, options.Password);
            if (!createResult.Succeeded)
            {
                _logger.LogWarning(
                    "Development seed user {Email} could not be created: {Errors}",
                    options.Email,
                    string.Join("; ", createResult.Errors.Select(static error => error.Description)));
                return false;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var requiresUpdate = false;

            requiresUpdate |= UpdateIfChanged(user, static value => value.FirstName, (entity, value) => entity.FirstName = value, options.FirstName);
            requiresUpdate |= UpdateIfChanged(user, static value => value.LastName, (entity, value) => entity.LastName = value, options.LastName);
            requiresUpdate |= UpdateIfChanged(user, static value => value.UserName, (entity, value) => entity.UserName = value, options.UserName);
            requiresUpdate |= UpdateIfChanged(user, static value => value.PhoneNumber, (entity, value) => entity.PhoneNumber = value, options.PhoneNumber);

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                requiresUpdate = true;
            }

            if (!user.IsActive)
            {
                user.IsActive = true;
                requiresUpdate = true;
            }

            if (user.Role != parsedRole)
            {
                user.Role = parsedRole;
                requiresUpdate = true;
            }

            if (requiresUpdate)
            {
                user.UpdatedAtUtc = DateTime.UtcNow;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogWarning(
                        "Development seed user {Email} could not be updated: {Errors}",
                        options.Email,
                        string.Join("; ", updateResult.Errors.Select(static error => error.Description)));
                    return false;
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var hasConfiguredPassword = await _userManager.CheckPasswordAsync(user, options.Password);
            if (!hasConfiguredPassword)
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, options.Password);
                if (!resetResult.Succeeded)
                {
                    _logger.LogWarning(
                        "Development seed user {Email} password could not be reset: {Errors}",
                        options.Email,
                        string.Join("; ", resetResult.Errors.Select(static error => error.Description)));
                    return false;
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removableRoles = currentRoles
            .Where(roleName =>
                BuiltInRoleNames.Contains(roleName) &&
                !string.Equals(roleName, desiredRoleName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (removableRoles.Length > 0)
        {
            var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, removableRoles);
            if (!removeRolesResult.Succeeded)
            {
                _logger.LogWarning(
                    "Development seed user {Email} could not remove stale roles: {Errors}",
                    options.Email,
                    string.Join("; ", removeRolesResult.Errors.Select(static error => error.Description)));
                return false;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        if (!currentRoles.Contains(desiredRoleName, StringComparer.OrdinalIgnoreCase))
        {
            var addRoleResult = await _userManager.AddToRoleAsync(user, desiredRoleName);
            if (!addRoleResult.Succeeded)
            {
                _logger.LogWarning(
                    "Development seed user {Email} could not be assigned role {Role}: {Errors}",
                    options.Email,
                    desiredRoleName,
                    string.Join("; ", addRoleResult.Errors.Select(static error => error.Description)));
                return false;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Development seed user {Email} is ready with role {Role}.",
            options.Email,
            desiredRoleName);

        return true;
    }

    private static bool UpdateIfChanged(
        User user,
        Func<User, string?> getter,
        Action<User, string> setter,
        string nextValue)
    {
        var normalizedCurrent = getter(user)?.Trim() ?? string.Empty;
        var normalizedNext = nextValue?.Trim() ?? string.Empty;

        if (string.Equals(normalizedCurrent, normalizedNext, StringComparison.Ordinal))
        {
            return false;
        }

        setter(user, normalizedNext);
        return true;
    }
}
