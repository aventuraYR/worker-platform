namespace WorkerPlatform.Core.Configuration;

public class VaultRuntimeOptions
{
    public int MaxRetryAttempts { get; set; } = 3;
    public int InitialRetryDelayMilliseconds { get; set; } = 500;
    public int RequestTimeoutSeconds { get; set; } = 10;
    public bool UseExponentialBackoff { get; set; } = true;
    public bool EnableLocalFallback { get; set; } = false;

    public void Validate()
    {
        if (MaxRetryAttempts <= 0)
            throw new InvalidOperationException(
                "Vault:Runtime:MaxRetryAttempts debe ser mayor a 0");

        if (InitialRetryDelayMilliseconds < 0)
            throw new InvalidOperationException(
                "Vault:Runtime:InitialRetryDelayMilliseconds no puede ser negativo");

        if (RequestTimeoutSeconds <= 0)
            throw new InvalidOperationException(
                "Vault:Runtime:RequestTimeoutSeconds debe ser mayor a 0");
    }
}
