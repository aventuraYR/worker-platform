using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WorkerPlatform.Core.Health;
using WorkerPlatform.Core.Middleware;
using WorkerPlatform.Core.Resilience;
using WorkerPlatform.Core.Configuration;
using WorkerPlatform.Core.Logging;

namespace WorkerPlatform.Core.Extensions;

public static class WorkerPlatformExtensions
{
    public static IHostApplicationBuilder AddWorkerPlatform(
        this IHostApplicationBuilder builder,
        string appName,
        params string[] sqlConnectionStringNames)
    {
        // Configuración + Vault
        builder.AddSharedConfiguration(appName);

        // Logging + Loki
        builder.AddSharedLogging(appName);

        // Resiliencia
        builder.Services.AddSharedResilience();

        // Health checks
        builder.AddSharedHealthChecks(sqlConnectionStringNames);

        // Job wrapper (disponible por DI en todos los jobs)
        builder.Services.AddSingleton<JobExecutionWrapper>();

        return builder;
    }
}