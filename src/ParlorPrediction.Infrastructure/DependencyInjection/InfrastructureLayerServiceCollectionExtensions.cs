using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ParlorPrediction.Application.Configuration;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Files;
using ParlorPrediction.Infrastructure.Services.Auth;
using ParlorPrediction.Infrastructure.Services.Storage;

namespace ParlorPrediction.Infrastructure.DependencyInjection;

public static class InfrastructureLayerServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<MailOptions>(configuration.GetSection("Mail"));
        services.Configure<FrontendOptions>(configuration.GetSection("Frontend"));
        services.Configure<TemplatePathOptions>(configuration.GetSection("TemplatePaths"));
        services.Configure<BootstrapAdminOptions>(configuration.GetSection("BootstrapAdmin"));
        services.Configure<DevelopmentSeedUsersOptions>(configuration.GetSection("DevelopmentSeedUsers"));

        var jwtKey = configuration["Jwt:Key"] ?? string.Empty;
        services.AddAuthentication()
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        services.AddScoped<ITokenProvider, JwtTokenProvider>();
        services.AddScoped<IFileStorage, AzureBlobFileStorage>();

        return services;
    }
}
