using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace WorkerPlatform.Core.Health;

public static class HealthCheckExtensions
{
    public static IHostApplicationBuilder AddSharedHealthChecks(
        this IHostApplicationBuilder builder,
        params string[] connectionStringNames)   // ← params acepta 0, 1 o muchos
    {
        var runtimeOptions = builder.Configuration
            .GetSection("Health:Runtime")
            .Get<HealthRuntimeOptions>() ?? new HealthRuntimeOptions();
        runtimeOptions.Validate();

        builder.Services.TryAddSingleton(runtimeOptions);
        builder.Services.TryAddSingleton<StartupState>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, StartupSignalHostedService>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, HealthPublisherHostedService>());

        var checks = builder.Services.AddHealthChecks();

        checks.AddCheck<StartupHealthCheck>(
            name: "startup-state",
            failureStatus: HealthStatus.Unhealthy,
            tags: [HealthProbeTags.Startup]);

        checks.AddCheck<LivenessHealthCheck>(
            name: "liveness-self",
            failureStatus: HealthStatus.Unhealthy,
            tags: [HealthProbeTags.Liveness]);

        checks.AddCheck<ReadinessHealthCheck>(
            name: "readiness-self",
            failureStatus: HealthStatus.Unhealthy,
            tags: [HealthProbeTags.Readiness]);

        foreach (var name in connectionStringNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var connString = builder.Configuration.GetConnectionString(name);

            if (!string.IsNullOrWhiteSpace(connString))
            {
                checks.Add(new HealthCheckRegistration(
                    name: $"sqlserver-{name}",
                    factory: _ => new SqlServerHealthCheck(connString),
                    failureStatus: HealthStatus.Unhealthy,
                    tags: [HealthProbeTags.Readiness, "db", "sql"]
                ));
            }
        }

        return builder;
    }
}
