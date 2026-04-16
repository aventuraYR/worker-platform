using Dapper;
using Microsoft.Data.SqlClient;
using WorkerPlatform.Jobs.Ejemplo.Models;

namespace WorkerPlatform.Jobs.Ejemplo.Repositories;

public class EjemploRepository : IEjemploRepository
{
    private readonly string _connectionString;
    private readonly ILogger<EjemploRepository> _logger;

    public EjemploRepository(
        IConfiguration config,
        ILogger<EjemploRepository> logger)
    {
        // Leer la configuración del job
        var jobConfig = config
            .GetSection("Job")
            .Get<JobConfiguration>()
            ?? throw new InvalidOperationException(
                "Sección 'Job' no encontrada en appsettings.json");

        jobConfig.ConnectionStrings.Validate();

        _connectionString = config
            .GetConnectionString(jobConfig.ConnectionStrings.DbProyecto5Test)
            ?? throw new InvalidOperationException(
                $"ConnectionString '{jobConfig.ConnectionStrings.DbProyecto5Test}' no encontrada en Vault");
        
        _logger = logger;
    }

    public async Task<IEnumerable<EjemploRegistro>> ObtenerPendientesAsync(
        CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
        return await conn.QueryAsync<EjemploRegistro>(
            new CommandDefinition(
                "SELECT Id, Descripcion, Estado, FechaCreacion " +
                "FROM Registros WHERE Estado = 'Pendiente' " +
                "ORDER BY FechaCreacion",
                cancellationToken: ct
            )
        );
    }

    public async Task<int> MarcarProcesadoAsync(int id, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
        return await conn.ExecuteAsync(
            new CommandDefinition(
                "UPDATE Registros SET Estado = 'Procesado', " +
                "FechaProcesado = GETUTCDATE() WHERE Id = @Id",
                new { Id = id },
                cancellationToken: ct
            )
        );
    }
}