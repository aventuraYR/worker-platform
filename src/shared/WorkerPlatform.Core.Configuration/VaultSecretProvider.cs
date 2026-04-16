using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace WorkerPlatform.Core.Configuration;

public class VaultSecretProvider
{
    private readonly IVaultClient _client;
    private readonly VaultConfiguration _config;

    public VaultSecretProvider(VaultConfiguration config)
    {
        _config = config;

        var settings = new VaultClientSettings(
            _config.Address,
            new TokenAuthMethodInfo(_config.Token)
        );

        _client = new VaultClient(settings);
    }

    public async Task<Dictionary<string, string>> GetConnectionStringsAsync()
    {
        try
        {
            var secret = await _client.V1.Secrets.KeyValue.V2
                .ReadSecretAsync(
                    path: _config.SecretPath,
                    mountPoint: _config.MountPoint
                );

            return secret.Data.Data
                .ToDictionary(
                    k => k.Key,
                    v => v.Value?.ToString() ?? string.Empty
                );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"No se pudo obtener secrets de Vault en {_config.Address}. " +
                "Verifica que Vault esté corriendo y el token sea válido.", ex);
        }
    }
}