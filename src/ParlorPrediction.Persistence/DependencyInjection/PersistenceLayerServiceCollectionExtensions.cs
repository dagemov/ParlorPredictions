using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Domain.Entities;
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
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDoughDemandPlanReadRepository, DoughDemandPlanRepository>();
        services.AddScoped<IDoughDemandPlanRepository, DoughDemandPlanRepository>();
        services.AddScoped<IDoughInventoryReadRepository, DoughInventoryReadRepository>();
        services.AddScoped<IDoughPrepRecommendationReadRepository, DoughPrepRecommendationReadRepository>();
        services.AddScoped<IDoughPrepRecommendationRepository, DoughPrepRecommendationRepository>();
        services.AddScoped<IPrepItemReadRepository, PrepItemReadRepository>();
        services.AddScoped<IPrepTaskRepository, PrepTaskRepository>();
        services.AddScoped<IRestaurantEventReadRepository, RestaurantEventRepository>();
        services.AddScoped<IRestaurantEventRepository, RestaurantEventRepository>();
        services.AddScoped<ISalesHistoryReadRepository, SalesHistoryReadRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<AuthBootstrapper>();

        return services;
    }
}
