using Microsoft.Extensions.DependencyInjection;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Application.Services.Dough;
using ParlorPrediction.Application.Services.Prep;

namespace ParlorPrediction.Application.DependencyInjection;

public static class ApplicationLayerServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<IDoughPrepCalculationService, DoughPrepCalculationService>();
        services.AddScoped<IDoughPrepRecommendationReadService, DoughPrepRecommendationReadService>();
        services.AddScoped<IDoughPrepRecommendationService, DoughPrepRecommendationService>();
        services.AddScoped<IPrepTaskReadService, PrepTaskReadService>();
        services.AddScoped<IPrepTaskService, PrepTaskService>();

        return services;
    }
}
