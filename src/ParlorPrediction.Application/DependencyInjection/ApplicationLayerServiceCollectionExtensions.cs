using Microsoft.Extensions.DependencyInjection;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Application.Services.Ai;
using ParlorPrediction.Application.Services.Dough;
using ParlorPrediction.Application.Services.Prep;

namespace ParlorPrediction.Application.DependencyInjection;

public static class ApplicationLayerServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<IAiPrepRecommendationService, AiPrepRecommendationService>();
        services.AddScoped<IDoughDemandPlanService, DoughDemandPlanService>();
        services.AddScoped<IDoughPrepCalculationService, DoughPrepCalculationService>();
        services.AddScoped<IDoughProductionPlanningService, DoughProductionPlanningService>();
        services.AddScoped<IDoughPrepRecommendationReadService, DoughPrepRecommendationReadService>();
        services.AddScoped<IDoughPrepRecommendationService, DoughPrepRecommendationService>();
        services.AddScoped<IRestaurantEventManagementService, RestaurantEventManagementService>();
        services.AddScoped<IPrepDashboardReadService, PrepDashboardReadService>();
        services.AddScoped<IPrepTaskReadService, PrepTaskReadService>();
        services.AddScoped<IPrepTaskService, PrepTaskService>();
        services.AddScoped<IPrepWeeklyDoughCalendarService, PrepWeeklyDoughCalendarService>();

        return services;
    }
}
