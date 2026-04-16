using Serilog;

namespace WorkerPlatform.Core.Logging;

public static class LoggingEnricher
{
    public static LoggerConfiguration EnrichWithAppContext(
        this LoggerConfiguration config,
        string appName,
        string environment)
    {
        return config
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()        // ← quita WithMachineName, era ambiguo
            .Enrich.WithProperty("Application", appName)
            .Enrich.WithProperty("Environment", environment)
            .Enrich.WithProperty("MachineName",  Environment.MachineName) // ← así evitamos ambigüedad
            .Enrich.WithProperty("Version",
                System.Reflection.Assembly
                    .GetEntryAssembly()
                    ?.GetName().Version?.ToString() ?? "unknown");
    }
}