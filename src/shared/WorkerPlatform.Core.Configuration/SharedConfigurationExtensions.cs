using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace WorkerPlatform.Core.Configuration;

public static class SharedConfigurationExtensions
{
    public static IHostApplicationBuilder AddSharedConfiguration(
    this IHostApplicationBuilder builder,
    string appName)
    {
        // 1. Ruta al archivo compartido — configurable por variable de entorno
        var sharedConfigPath = Environment.GetEnvironmentVariable("SHARED_CONFIG_PATH")
            ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "config");

        var sharedConfigFile = Path.Combine(sharedConfigPath, "appsettings.shared.json");

        builder.Configuration
            // Primero el archivo compartido (base para todas las apps)
            .AddJsonFile(sharedConfigFile, optional: false, reloadOnChange: true)
            // Luego el local (puede sobreescribir valores del compartido)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            // Variables de entorno tienen la máxima prioridad
            .AddEnvironmentVariables();

        // 2. Leer opciones de Vault desde config
        var vaultConfig = builder.Configuration
            .GetSection("Vault")
            .Get<VaultConfiguration>() ?? new VaultConfiguration();

        vaultConfig.Validate();

        // 3. Obtener connection strings desde Vault
        var provider = new VaultSecretProvider(vaultConfig);

        var connectionStrings = provider
            .GetConnectionStringsAsync()
            .GetAwaiter()
            .GetResult();

        // 4. Inyectar como ConnectionStrings estándar de .NET
        var inMemory = connectionStrings.ToDictionary(
            k => $"ConnectionStrings:{k.Key}",
            v => (string?)v.Value
        );

        // 5. Nombre de la app para logging y telemetría
        inMemory["AppName"] = appName;

        builder.Configuration.AddInMemoryCollection(inMemory);

        return builder;
    }
}