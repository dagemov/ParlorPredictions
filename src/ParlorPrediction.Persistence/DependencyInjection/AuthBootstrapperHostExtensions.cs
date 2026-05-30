using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ParlorPrediction.Persistence.Services;

namespace ParlorPrediction.Persistence.DependencyInjection;

public static class AuthBootstrapperHostExtensions
{
    public static async Task InitializeAuthBootstrapAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        using var scope = host.Services.CreateScope();
        var bootstrapper = scope.ServiceProvider.GetRequiredService<AuthBootstrapper>();
        await bootstrapper.InitializeAsync(cancellationToken);
    }
}
