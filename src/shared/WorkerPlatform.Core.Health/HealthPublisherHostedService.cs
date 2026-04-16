using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WorkerPlatform.Core.Health;

internal sealed class HealthPublisherHostedService : BackgroundService
{
    private readonly HealthCheckService _healthCheckService;
    private readonly HealthRuntimeOptions _options;
    private readonly ILogger<HealthPublisherHostedService> _logger;

    public HealthPublisherHostedService(
        HealthCheckService healthCheckService,
        HealthRuntimeOptions options,
        ILogger<HealthPublisherHostedService> logger)
    {
        _healthCheckService = healthCheckService;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!_options.EnablePublisher)
            {
                _logger.LogInformation(
                    "Publicador de health checks deshabilitado por configuración");
                return;
            }

            if (_options.InitialDelaySeconds > 0)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.InitialDelaySeconds),
                    stoppingToken);
            }

            _logger.LogInformation(
                "Publicador de health checks iniciado. Intervalo: {Intervalo}s",
                _options.IntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                await PublishProbeAsync(HealthProbeTags.Startup, stoppingToken);
                await PublishProbeAsync(HealthProbeTags.Liveness, stoppingToken);
                await PublishProbeAsync(HealthProbeTags.Readiness, stoppingToken);

                await Task.Delay(
                    TimeSpan.FromSeconds(_options.IntervalSeconds),
                    stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Publicador de health checks cancelado por apagado del host");
        }
    }

    private async Task PublishProbeAsync(
        string probeTag,
        CancellationToken ct)
    {
        using var activity = HealthTelemetry.StartProbeActivity(probeTag);
        var sw = Stopwatch.StartNew();

        var report = await _healthCheckService.CheckHealthAsync(
            registration => registration.Tags.Contains(probeTag),
            ct);

        sw.Stop();

        activity?.SetTag("health.status", report.Status.ToString());
        activity?.SetTag("health.checks.count", report.Entries.Count);
        HealthTelemetry.RecordProbe(
            probeTag,
            report.Status,
            sw.Elapsed,
            report.Entries.Count);

        if (_options.LogOnlyUnhealthy && report.Status == HealthStatus.Healthy)
            return;

        var failedChecks = report.Entries
            .Where(x => x.Value.Status != HealthStatus.Healthy)
            .Select(x => x.Key)
            .ToArray();

        if (report.Status == HealthStatus.Healthy)
        {
            _logger.LogInformation(
                "Probe {Probe} saludable en {DurationMs}ms ({Checks} check(s))",
                probeTag,
                sw.Elapsed.TotalMilliseconds,
                report.Entries.Count);
            return;
        }

        _logger.LogWarning(
            "Probe {Probe} en estado {Status} en {DurationMs}ms. Fallos: {FailedChecks}",
            probeTag,
            report.Status,
            sw.Elapsed.TotalMilliseconds,
            failedChecks);
    }
}
