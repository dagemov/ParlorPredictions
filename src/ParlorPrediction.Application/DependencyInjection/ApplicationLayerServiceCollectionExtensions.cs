using Microsoft.Extensions.DependencyInjection;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Application.Services.Auth;
using ParlorPrediction.Application.Services.Ai;
using ParlorPrediction.Application.Services.Dough;
using ParlorPrediction.Application.Services.Prep;

namespace ParlorPrediction.Application.DependencyInjection;

public static class ApplicationLayerServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<IAiPrepRecommendationService, AiPrepRecommendationService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IDoughDemandPlanService, DoughDemandPlanService>();
        services.AddScoped<IDoughSourceProjectionService, DoughSourceProjectionService>();
        services.AddScoped<IDoughAvailabilityProjectionService, DoughAvailabilityProjectionService>();
        services.AddScoped<IDoughPrepCalculationService, DoughPrepCalculationService>();
        services.AddScoped<IDoughQualityManagementService, DoughQualityManagementService>();
        services.AddScoped<IDoughQualityReadService, DoughQualityReadService>();
        services.AddScoped<IDoughUsageTraceManagementService, DoughUsageTraceManagementService>();
        services.AddScoped<IDoughUsageTraceReadService, DoughUsageTraceReadService>();
        services.AddScoped<IDailyDoughClosingManagementService, DailyDoughClosingManagementService>();
        services.AddScoped<IDailyDoughClosingReadService, DailyDoughClosingReadService>();
        services.AddScoped<IWeeklyDoughClosingManagementService, WeeklyDoughClosingManagementService>();
        services.AddScoped<IWeeklyDoughClosingReadService, WeeklyDoughClosingReadService>();
        services.AddScoped<IDoughProductionPlanningService, DoughProductionPlanningService>();
        services.AddScoped<IDoughPrepRecommendationReadService, DoughPrepRecommendationReadService>();
        services.AddScoped<IDoughPrepRecommendationService, DoughPrepRecommendationService>();
        services.AddScoped<IManagerPrepRecommendationService, ManagerPrepRecommendationService>();
        services.AddScoped<IRestaurantEventManagementService, RestaurantEventManagementService>();
        services.AddScoped<IPrepCatalogReadService, PrepCatalogReadService>();
        services.AddScoped<IPrepDashboardReadService, PrepDashboardReadService>();
        services.AddScoped<IPrepTaskReadService, PrepTaskReadService>();
        services.AddScoped<IPrepTaskService, PrepTaskService>();
        services.AddScoped<IPrepWeeklyDoughCalendarService, PrepWeeklyDoughCalendarService>();

        return services;
    }
}
