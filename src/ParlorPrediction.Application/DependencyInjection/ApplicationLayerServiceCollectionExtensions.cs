using Microsoft.Extensions.DependencyInjection;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Services.Dough;

namespace ParlorPrediction.Application.DependencyInjection;

public static class ApplicationLayerServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<IDoughPrepCalculationService, DoughPrepCalculationService>();

        return services;
    }
}
