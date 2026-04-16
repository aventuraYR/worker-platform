using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Globalization;

namespace WorkerPlatform.Core.Configuration;

public static class SharedConfigurationExtensions
{
    public static IHostApplicationBuilder AddSharedConfiguration(
    this IHostApplicationBuilder builder,
    string appName)
    {
        // 1. Ruta al archivo compartido
        var sharedConfigFile = ResolveSharedConfigFilePath();

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

        var connectionStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var source = "vault";
        var vaultAttempts = 0;
        var vaultLoadDurationMs = 0d;

        try
        {
            var result = provider
                .GetConnectionStringsAsync()
                .GetAwaiter()
                .GetResult();

            connectionStrings = new Dictionary<string, string>(
                result.ConnectionStrings,
                StringComparer.OrdinalIgnoreCase);

            vaultAttempts = result.Attempts;
            vaultLoadDurationMs = result.Duration.TotalMilliseconds;

            if (connectionStrings.Count == 0 && vaultConfig.Runtime.EnableLocalFallback)
            {
                connectionStrings = GetLocalConnectionStrings(builder.Configuration);
                source = "local-fallback-empty-vault";
            }
        }
        catch (Exception ex) when (vaultConfig.Runtime.EnableLocalFallback)
        {
            connectionStrings = GetLocalConnectionStrings(builder.Configuration);
            if (connectionStrings.Count == 0)
            {
                throw new InvalidOperationException(
                    "Vault falló y el fallback local está habilitado, " +
                    "pero no existen valores en ConnectionStrings.",
                    ex);
            }

            source = "local-fallback";
            vaultAttempts = vaultConfig.Runtime.MaxRetryAttempts;
            Console.Error.WriteLine(
                $"[WorkerPlatform.Core] Vault no disponible. Se utilizará fallback local. Error: {ex.Message}");
        }

        // 4. Inyectar como ConnectionStrings estándar de .NET
        var inMemory = connectionStrings.ToDictionary(
            k => $"ConnectionStrings:{k.Key}",
            v => (string?)v.Value
        );

        // 5. Nombre de la app para logging y telemetría
        inMemory["AppName"] = appName;
        inMemory["WorkerPlatform:ConfigurationSource"] = source;
        inMemory["WorkerPlatform:VaultAttempts"] =
            vaultAttempts.ToString(CultureInfo.InvariantCulture);
        inMemory["WorkerPlatform:VaultLoadDurationMs"] =
            vaultLoadDurationMs.ToString("F0", CultureInfo.InvariantCulture);

        builder.Configuration.AddInMemoryCollection(inMemory);

        return builder;
    }

    private static string ResolveSharedConfigFilePath()
    {
        var configuredPath = Environment.GetEnvironmentVariable("SHARED_CONFIG_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var envCandidate = Path.Combine(configuredPath, "appsettings.shared.json");
            if (File.Exists(envCandidate))
                return envCandidate;

            throw new FileNotFoundException(
                $"No se encontró appsettings.shared.json en SHARED_CONFIG_PATH: {envCandidate}");
        }

        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
        var cursor = currentDirectory;

        while (cursor is not null)
        {
            var candidate = Path.Combine(cursor.FullName, "config", "appsettings.shared.json");
            if (File.Exists(candidate))
                return candidate;

            cursor = cursor.Parent;
        }

        var fallbackPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "..",
            "config",
            "appsettings.shared.json"));

        if (File.Exists(fallbackPath))
            return fallbackPath;

        throw new FileNotFoundException(
            "No se encontró appsettings.shared.json. " +
            "Define SHARED_CONFIG_PATH o coloca el archivo en la carpeta config del repositorio.");
    }

    private static Dictionary<string, string> GetLocalConnectionStrings(
        IConfiguration configuration)
    {
        return configuration
            .GetSection("ConnectionStrings")
            .GetChildren()
            .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
            .ToDictionary(
                k => k.Key,
                v => v.Value!,
                StringComparer.OrdinalIgnoreCase);
    }
}
