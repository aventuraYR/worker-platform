using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace WorkerPlatform.Core.Health;

public static class HealthCheckExtensions
{
    public static IHostApplicationBuilder AddSharedHealthChecks(
        this IHostApplicationBuilder builder,
        params string[] connectionStringNames)   // ← params acepta 0, 1 o muchos
    {
        var checks = builder.Services.AddHealthChecks();

        foreach (var name in connectionStringNames)
        {
            var connString = builder.Configuration
                .GetSection($"ConnectionStrings:{name}")
                .Value;

            if (!string.IsNullOrWhiteSpace(connString))
            {
                checks.Add(new HealthCheckRegistration(
                    name: $"sqlserver-{name}",
                    factory: _ => new SqlServerHealthCheck(connString),
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["db", "sql"]
                ));
            }
        }

        return builder;
    }
}