using Microsoft.Extensions.DependencyInjection;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Services.Auth;

namespace ParlorPrediction.Application.DependencyInjection;

public static class UserLayerServiceCollectionExtensions
{
    public static IServiceCollection AddUserLayer(this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IUserTokenService, UserTokenService>();

        return services;
    }
}
