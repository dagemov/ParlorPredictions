using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Persistence.Identity;
using ParlorPrediction.Persistence.Repositories;
using ParlorPrediction.Persistence.Services;

namespace ParlorPrediction.Persistence.DependencyInjection;

public static class PersistenceLayerServiceCollectionExtensions
{
    public static IServiceCollection AddPersistenceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ParlorPredictionDb")
            ?? throw new InvalidOperationException("ConnectionStrings:ParlorPredictionDb is required.");

        services.AddDbContext<ParlorPredictionDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddIdentity<User, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 0;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddUserStore<ParlorPredictionUserStore>()
            .AddRoleStore<ParlorPredictionRoleStore>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/session/login";
            options.AccessDeniedPath = "/session/access-denied";
            options.Cookie.Name = "ParlorPrediction.Auth";
            options.SlidingExpiration = true;
            options.Events.OnValidatePrincipal = async context =>
            {
                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
                var principal = context.Principal;
                var user = principal is null ? null : await userManager.GetUserAsync(principal);

                if (user is null || !user.IsActive)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync();
                    return;
                }

                var currentRoleClaim = principal?.FindFirstValue(ClaimTypes.Role);
                if (!string.Equals(currentRoleClaim, user.Role.GetCanonicalName(), StringComparison.OrdinalIgnoreCase))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync();
                }
            };
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDoughBatchRepository, DoughBatchRepository>();
        services.AddScoped<IDoughBatchReadRepository, DoughBatchReadRepository>();
        services.AddScoped<IDoughBatchQualityRepository, DoughBatchQualityRepository>();
        services.AddScoped<IDoughDemandPlanReadRepository, DoughDemandPlanRepository>();
        services.AddScoped<IDoughDemandPlanRepository, DoughDemandPlanRepository>();
        services.AddScoped<IDoughInventorySnapshotRepository, DoughInventorySnapshotRepository>();
        services.AddScoped<IDoughInventoryReadRepository, DoughInventoryReadRepository>();
        services.AddScoped<IDoughLossRecordRepository, DoughLossRecordRepository>();
        services.AddScoped<IDoughPrepRecommendationReadRepository, DoughPrepRecommendationReadRepository>();
        services.AddScoped<IDoughPrepRecommendationRepository, DoughPrepRecommendationRepository>();
        services.AddScoped<IDoughReballRecordRepository, DoughReballRecordRepository>();
        services.AddScoped<IDoughUsageTraceRepository, DoughUsageTraceRepository>();
        services.AddScoped<IDailyDoughClosingRepository, DailyDoughClosingRepository>();
        services.AddScoped<IWeeklyDoughClosingRepository, WeeklyDoughClosingRepository>();
        services.AddScoped<IManagerPrepRecommendationRepository, ManagerPrepRecommendationRepository>();
        services.AddScoped<IPrepItemReadRepository, PrepItemReadRepository>();
        services.AddScoped<IPrepStationReadRepository, PrepStationReadRepository>();
        services.AddScoped<IPrepTaskRepository, PrepTaskRepository>();
        services.AddScoped<IRestaurantEventReadRepository, RestaurantEventRepository>();
        services.AddScoped<IRestaurantEventRepository, RestaurantEventRepository>();
        services.AddScoped<ISalesHistoryReadRepository, SalesHistoryReadRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<AuthBootstrapper>();

        return services;
    }
}
