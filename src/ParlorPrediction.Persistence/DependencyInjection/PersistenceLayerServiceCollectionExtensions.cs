using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Domain.Entities;
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
            .AddEntityFrameworkStores<ParlorPredictionDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<AuthBootstrapper>();

        return services;
    }
}
