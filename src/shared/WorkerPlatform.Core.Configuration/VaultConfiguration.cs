namespace WorkerPlatform.Core.Configuration;

public class VaultConfiguration
{
    public string Address    { get; set; } = string.Empty;
    public string MountPoint { get; set; } = string.Empty;
    public string SecretPath { get; set; } = string.Empty;
    public VaultRuntimeOptions Runtime { get; set; } = new();

    public string Token =>
        Environment.GetEnvironmentVariable("VAULT_APP_TOKEN")
        ?? throw new InvalidOperationException(
            "Variable de entorno VAULT_APP_TOKEN no encontrada. " +
            "Configúrala con: setx VAULT_APP_TOKEN <token> /M");

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Address))
            throw new InvalidOperationException(
                "Vault:Address no está configurado en appsettings.json");

        if (string.IsNullOrWhiteSpace(MountPoint))
            throw new InvalidOperationException(
                "Vault:MountPoint no está configurado en appsettings.json");

        if (string.IsNullOrWhiteSpace(SecretPath))
            throw new InvalidOperationException(
                "Vault:SecretPath no está configurado en appsettings.json");

        Runtime.Validate();
    }
}
