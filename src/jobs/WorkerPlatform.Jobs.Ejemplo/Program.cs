using Serilog;
using Serilog.Core;
using WorkerPlatform.Core.Extensions;
using WorkerPlatform.Jobs.Ejemplo.Jobs;
using WorkerPlatform.Jobs.Ejemplo.Models;
using WorkerPlatform.Jobs.Ejemplo.Repositories;

var builder = Host.CreateApplicationBuilder(args);

// Leer nombres de cadenas de conexión desde config
var jobConfig = builder.Configuration
    .GetSection("Job")
    .Get<JobConfiguration>() ?? new JobConfiguration();

jobConfig.ConnectionStrings.Validate();

// Una sola línea configura todo:
// Vault + Logging + Resilience + HealthChecks + JobWrapper
builder.AddWorkerPlatform(
    appName: "WorkerPlatform.Jobs.Ejemplo",
    sqlConnectionStringNames: new[] {
        jobConfig.ConnectionStrings.DbProyecto5Test
    }.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
);

// Repositorios
builder.Services.AddScoped<IEjemploRepository, EjemploRepository>();

// Elige UNO según el tipo de job:
builder.Services.AddHostedService<EjemploProgramadoJob>();  // ← Task Scheduler
// builder.Services.AddHostedService<EjemploContinuoJob>(); // ← Siempre corriendo

try
{
    await builder.Build().RunAsync();    
}
finally
{
    await Log.CloseAndFlushAsync();
}