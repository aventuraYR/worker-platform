using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WorkerPlatform.Core.Health;

internal sealed class StartupHealthCheck : IHealthCheck
{
    private readonly StartupState _startupState;

    public StartupHealthCheck(StartupState startupState)
    {
        _startupState = startupState;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_startupState.IsStarted)
            return Task.FromResult(HealthCheckResult.Healthy("Startup completado"));

        return Task.FromResult(HealthCheckResult.Unhealthy(
            "La aplicación aún se encuentra en fase de startup"));
    }
}
