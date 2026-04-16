using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WorkerPlatform.Core.Health;

internal sealed class ReadinessHealthCheck : IHealthCheck
{
    private readonly StartupState _startupState;

    public ReadinessHealthCheck(StartupState startupState)
    {
        _startupState = startupState;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_startupState.IsStarted)
            return Task.FromResult(HealthCheckResult.Healthy("Aplicación lista para procesar"));

        return Task.FromResult(HealthCheckResult.Unhealthy(
            "Readiness no disponible durante startup"));
    }
}
