using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WorkerPlatform.Core.Health;

internal sealed class StartupSignalHostedService : IHostedService
{
    private readonly StartupState _startupState;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<StartupSignalHostedService> _logger;

    public StartupSignalHostedService(
        StartupState startupState,
        IHostApplicationLifetime lifetime,
        ILogger<StartupSignalHostedService> logger)
    {
        _startupState = startupState;
        _lifetime = lifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStarted.Register(() =>
        {
            _startupState.MarkStarted();
            _logger.LogInformation("Estado de startup marcado como completado");
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
