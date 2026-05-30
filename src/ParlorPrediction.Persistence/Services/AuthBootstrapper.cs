using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParlorPrediction.Application.Configuration;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Persistence.Services;

public sealed class AuthBootstrapper
{
    private readonly ILogger<AuthBootstrapper> _logger;
    private readonly ParlorPredictionDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly BootstrapAdminOptions _bootstrapAdminOptions;

    public AuthBootstrapper(
        ILogger<AuthBootstrapper> logger,
        ParlorPredictionDbContext dbContext,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<BootstrapAdminOptions> bootstrapAdminOptions)
    {
        _logger = logger;
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _bootstrapAdminOptions = bootstrapAdminOptions.Value;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.EnsureCreatedAsync(cancellationToken);

        foreach (var builtInRoleName in ApplicationRoleExtensions.GetAllNames())
        {
            if (!await _roleManager.RoleExistsAsync(builtInRoleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(builtInRoleName));
            }
        }

        if (string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Email))
        {
            return;
        }

        var existingUser = await _userManager.FindByEmailAsync(_bootstrapAdminOptions.Email);
        if (existingUser is not null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Password))
        {
            _logger.LogWarning("Bootstrap admin password is not configured. Manager seed was skipped.");
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
            return;
        }

        await _userManager.AddToRoleAsync(user, roleName);
    }
}
