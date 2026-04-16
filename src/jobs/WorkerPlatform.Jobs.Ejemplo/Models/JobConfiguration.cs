namespace WorkerPlatform.Jobs.Ejemplo.Models;

public class JobConfiguration
{
    public JobConnectionStrings ConnectionStrings { get; set; } = new();
    public int IntervalSegundos { get; set; } = 60;
    public int TimeoutMinutos { get; set; } = 30;
}

public class JobConnectionStrings
{
    public string DbProyecto5Test { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DbProyecto5Test))
            throw new InvalidOperationException(
                "Job:ConnectionStrings:DbProyecto5Test no configurado en appsettings.json");
    }
}