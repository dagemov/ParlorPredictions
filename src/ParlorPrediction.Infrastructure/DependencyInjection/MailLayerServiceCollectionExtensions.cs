using Microsoft.Extensions.DependencyInjection;
using ParlorPrediction.Application.Interfaces.Common;
using ParlorPrediction.Infrastructure.Services.Mail;
using ParlorPrediction.Infrastructure.Services.Templates;

namespace ParlorPrediction.Infrastructure.DependencyInjection;

public static class MailLayerServiceCollectionExtensions
{
    public static IServiceCollection AddMailLayer(this IServiceCollection services)
    {
        services.AddScoped<IEmailSender, MailKitEmailSender>();
        services.AddScoped<ITemplateProvider, FileTemplateProvider>();

        return services;
    }
}
