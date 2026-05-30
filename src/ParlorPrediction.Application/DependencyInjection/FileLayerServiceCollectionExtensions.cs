using Microsoft.Extensions.DependencyInjection;
using ParlorPrediction.Application.Interfaces.Files;
using ParlorPrediction.Application.Services.Files;

namespace ParlorPrediction.Application.DependencyInjection;

public static class FileLayerServiceCollectionExtensions
{
    public static IServiceCollection AddFileLayer(this IServiceCollection services)
    {
        services.AddScoped<IFileBlobService, FileBlobService>();
        services.AddScoped<IValidationFileService, ValidationFileService>();

        return services;
    }
}
