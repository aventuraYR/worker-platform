using System.Diagnostics;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace WorkerPlatform.Core.Configuration;

public class VaultSecretProvider
{
    private readonly VaultConfiguration _config;

    public VaultSecretProvider(VaultConfiguration config)
    {
        _config = config;
    }

    public async Task<VaultFetchResult> GetConnectionStringsAsync(
        CancellationToken ct = default)
    {
        var totalTimer = Stopwatch.StartNew();
        Exception? lastException = null;

        for (var attempt = 1; attempt <= _config.Runtime.MaxRetryAttempts; attempt++)
        {
            using var activity = VaultTelemetry.StartReadActivity(
                _config.Address,
                _config.MountPoint,
                _config.SecretPath,
                attempt);

            try
            {
                var connectionStrings = await ReadConnectionStringsWithTimeoutAsync(ct);

                activity?.SetTag("vault.result", "success");
                VaultTelemetry.RecordSuccess(attempt, totalTimer.Elapsed);

                return new VaultFetchResult(
                    connectionStrings,
                    attempt,
                    totalTimer.Elapsed);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                activity?.SetTag("vault.result", "failure");
                activity?.SetTag("vault.error", ex.Message);
                VaultTelemetry.RecordRetry(attempt, ex.GetType().Name);

                if (attempt >= _config.Runtime.MaxRetryAttempts)
                    break;

                var delay = GetRetryDelay(attempt);
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, ct);
            }
        }

        VaultTelemetry.RecordFailure(_config.Runtime.MaxRetryAttempts, totalTimer.Elapsed);

        throw new InvalidOperationException(
            $"No se pudo obtener secrets de Vault en {_config.Address} " +
            $"después de {_config.Runtime.MaxRetryAttempts} intento(s). " +
            "Verifica que Vault esté corriendo, el token sea válido y la ruta del secret exista.",
            lastException);
    }

    private async Task<Dictionary<string, string>> ReadConnectionStringsWithTimeoutAsync(
        CancellationToken ct)
    {
        var client = CreateClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_config.Runtime.RequestTimeoutSeconds));

        try
        {
            var secretTask = client.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                path: _config.SecretPath,
                mountPoint: _config.MountPoint);

            var secret = await secretTask.WaitAsync(timeoutCts.Token);

            return secret.Data?.Data?.ToDictionary(
                       k => k.Key,
                       v => v.Value?.ToString() ?? string.Empty,
                       StringComparer.OrdinalIgnoreCase)
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Timeout al consultar Vault después de {_config.Runtime.RequestTimeoutSeconds}s.",
                ex);
        }
    }

    private IVaultClient CreateClient()
    {
        var settings = new VaultClientSettings(
            _config.Address,
            new TokenAuthMethodInfo(_config.Token));

        return new VaultClient(settings);
    }

    private TimeSpan GetRetryDelay(int attempt)
    {
        var baseDelay = TimeSpan.FromMilliseconds(_config.Runtime.InitialRetryDelayMilliseconds);
        if (baseDelay <= TimeSpan.Zero)
            return TimeSpan.Zero;

        if (!_config.Runtime.UseExponentialBackoff)
            return baseDelay;

        var factor = Math.Pow(2, attempt - 1);
        var totalMs = baseDelay.TotalMilliseconds * factor;

        return TimeSpan.FromMilliseconds(totalMs);
    }
}

public sealed record VaultFetchResult(
    IReadOnlyDictionary<string, string> ConnectionStrings,
    int Attempts,
    TimeSpan Duration);
