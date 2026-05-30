using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParlorPrediction.Application.Configuration;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Persistence.Services;

public sealed class AuthBootstrapper
{
    private readonly ILogger<AuthBootstrapper> _logger;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly BootstrapAdminOptions _bootstrapAdminOptions;
    private readonly IUnitOfWork _unitOfWork;

    public AuthBootstrapper(
        ILogger<AuthBootstrapper> logger,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<BootstrapAdminOptions> bootstrapAdminOptions,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _userManager = userManager;
        _roleManager = roleManager;
        _bootstrapAdminOptions = bootstrapAdminOptions.Value;
        _unitOfWork = unitOfWork;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var createdAnyRole = false;
            foreach (var builtInRoleName in ApplicationRoleExtensions.GetAllNames())
            {
                if (!await _roleManager.RoleExistsAsync(builtInRoleName))
                {
                    var roleCreationResult = await _roleManager.CreateAsync(new IdentityRole(builtInRoleName));
                    if (!roleCreationResult.Succeeded)
                    {
                        _logger.LogWarning(
                            "Bootstrap role {RoleName} could not be created: {Errors}",
                            builtInRoleName,
                            string.Join("; ", roleCreationResult.Errors.Select(static error => error.Description)));
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return;
                    }

                    createdAnyRole = true;
                }
            }

            if (createdAnyRole)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Email))
            {
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return;
            }

            var existingUser = await _userManager.FindByEmailAsync(_bootstrapAdminOptions.Email);
            if (existingUser is not null)
            {
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Password))
            {
                _logger.LogWarning("Bootstrap admin password is not configured. Manager seed was skipped.");
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return;
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
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var addToRoleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!addToRoleResult.Succeeded)
            {
                _logger.LogWarning(
                    "Bootstrap admin role assignment failed: {Errors}",
                    string.Join("; ", addToRoleResult.Errors.Select(static error => error.Description)));
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
}
