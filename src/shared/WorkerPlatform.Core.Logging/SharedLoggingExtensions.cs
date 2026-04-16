using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

namespace WorkerPlatform.Core.Logging;

public static class SharedLoggingExtensions
{
    public static IHostApplicationBuilder AddSharedLogging(
        this IHostApplicationBuilder builder,
        string appName)
    {
        var environment = builder.Environment.EnvironmentName;
        var lokiUrl     = builder.Configuration["Loki:Url"]
                          ?? "http://localhost:3100";

        var labels = new[]
        {
            new LokiLabel { Key = "app",         Value = appName },
            new LokiLabel { Key = "environment", Value = environment },
            new LokiLabel { Key = "server",      Value = Environment.MachineName }
        };

        builder.Services.AddSerilog(logConfig => logConfig
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System",    LogEventLevel.Warning)
            .EnrichWithAppContext(appName, environment)
            .WriteTo.Console(
                outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] [{Application}] " +
                    "{Message:lj}{NewLine}{Exception}"
            )
            .WriteTo.GrafanaLoki(
                uri: lokiUrl,
                labels: labels
            )
        );

        return builder;
    }
}