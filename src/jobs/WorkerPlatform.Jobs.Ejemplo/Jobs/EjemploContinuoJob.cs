using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkerPlatform.Core.Middleware;
using WorkerPlatform.Jobs.Ejemplo.Repositories;

namespace WorkerPlatform.Jobs.Ejemplo.Jobs;

public class EjemploContinuoJob : BackgroundService
{
    private readonly ILogger<EjemploContinuoJob> _logger;
    private readonly JobExecutionWrapper _wrapper;
    private readonly IEjemploRepository _repo;
    private readonly TimeSpan _interval;

    public EjemploContinuoJob(
        ILogger<EjemploContinuoJob> logger,
        JobExecutionWrapper wrapper,
        IEjemploRepository repo,
        IConfiguration config)
    {
        _logger   = logger;
        _wrapper  = wrapper;
        _repo     = repo;
        _interval = TimeSpan.FromSeconds(
            config.GetValue<int>("Job:IntervalSegundos", 60));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "Job continuo iniciado. Intervalo: {Intervalo}s",
            _interval.TotalSeconds);

        while (!ct.IsCancellationRequested)
        {
            await _wrapper.ExecuteAsync(
                "EjemploContinuo",
                async token =>
                {
                    var registros = await _repo.ObtenerPendientesAsync(token);
                    var lista     = registros.ToList();

                    if (lista.Count == 0)
                    {
                        _logger.LogInformation("Sin registros pendientes");
                        return;
                    }

                    foreach (var registro in lista)
                    {
                        await _repo.MarcarProcesadoAsync(registro.Id, token);

                        _logger.LogInformation(
                            "Registro {Id} procesado: {Descripcion}",
                            registro.Id, registro.Descripcion);
                    }
                },
                ct);

            // Esperar antes de la próxima iteración
            await Task.Delay(_interval, ct);
        }
    }
}