using WorkerPlatform.Jobs.Ejemplo.Models;

namespace WorkerPlatform.Jobs.Ejemplo.Repositories;

public interface IEjemploRepository
{
    Task<IEnumerable<EjemploRegistro>> ObtenerPendientesAsync(CancellationToken ct);
    Task<int> MarcarProcesadoAsync(int id, CancellationToken ct);
}