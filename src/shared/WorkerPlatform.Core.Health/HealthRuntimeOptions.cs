namespace WorkerPlatform.Core.Health;

public class HealthRuntimeOptions
{
    public bool EnablePublisher { get; set; } = true;
    public int InitialDelaySeconds { get; set; } = 15;
    public int IntervalSeconds { get; set; } = 60;
    public bool LogOnlyUnhealthy { get; set; } = false;

    public void Validate()
    {
        if (InitialDelaySeconds < 0)
            throw new InvalidOperationException(
                "Health:Runtime:InitialDelaySeconds no puede ser negativo");

        if (IntervalSeconds <= 0)
            throw new InvalidOperationException(
                "Health:Runtime:IntervalSeconds debe ser mayor a 0");
    }
}
