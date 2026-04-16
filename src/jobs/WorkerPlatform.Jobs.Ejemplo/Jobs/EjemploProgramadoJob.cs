using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkerPlatform.Core.Middleware;
using WorkerPlatform.Jobs.Ejemplo.Repositories;

namespace WorkerPlatform.Jobs.Ejemplo.Jobs;

public class EjemploProgramadoJob : BackgroundService
{
    private readonly ILogger<EjemploProgramadoJob> _logger;
    private readonly JobExecutionWrapper _wrapper;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IServiceScopeFactory _scopeFactory;

    public EjemploProgramadoJob(
        ILogger<EjemploProgramadoJob> logger,
        JobExecutionWrapper wrapper,
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime lifetime)
    {
        _logger   = logger;
        _wrapper  = wrapper;
        _scopeFactory = scopeFactory;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        /*var result = await _wrapper.ExecuteAsync(
            "EjemploProgramado",
            async token =>
            {
                throw new Exception("Error de prueba para validar alerta");
            },
            ct);*/

        var result = await _wrapper.ExecuteAsync(
            "EjemploProgramado",
            async token =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider
                    .GetRequiredService<IEjemploRepository>();

                var registros = await repo.ObtenerPendientesAsync(token);
                var lista     = registros.ToList();

                _logger.LogInformation(
                    "Procesando {Total} registros pendientes", lista.Count);

                foreach (var registro in lista)
                {
                    await repo.MarcarProcesadoAsync(registro.Id, token);

                    _logger.LogInformation(
                        "Registro {Id} procesado: {Descripcion}",
                        registro.Id, registro.Descripcion);
                }
            },
            ct);

        _lifetime.StopApplication();

        Environment.ExitCode = result.IsSuccess ? 0 : 1;
    }
}