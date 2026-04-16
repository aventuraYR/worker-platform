using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace WorkerPlatform.Core.Configuration;

internal static class VaultTelemetry
{
    private static readonly Meter Meter = new(
        "WorkerPlatform.Core.Configuration",
        "1.0.0");

    private static readonly Counter<long> SuccessCounter = Meter.CreateCounter<long>(
        "workerplatform.vault.load.success");

    private static readonly Counter<long> FailureCounter = Meter.CreateCounter<long>(
        "workerplatform.vault.load.failure");

    private static readonly Counter<long> RetryCounter = Meter.CreateCounter<long>(
        "workerplatform.vault.load.retry");

    private static readonly Histogram<double> DurationHistogram = Meter.CreateHistogram<double>(
        "workerplatform.vault.load.duration.ms");

    private static readonly ActivitySource ActivitySource = new(
        "WorkerPlatform.Core.Configuration.Vault");

    public static Activity? StartReadActivity(
        string address,
        string mountPoint,
        string secretPath,
        int attempt)
    {
        var activity = ActivitySource.StartActivity(
            "vault.read_connection_strings",
            ActivityKind.Client);

        activity?.SetTag("vault.address", address);
        activity?.SetTag("vault.mount_point", mountPoint);
        activity?.SetTag("vault.secret_path", secretPath);
        activity?.SetTag("vault.attempt", attempt);

        return activity;
    }

    public static void RecordSuccess(int attempt, TimeSpan duration)
    {
        SuccessCounter.Add(1, new KeyValuePair<string, object?>("attempt", attempt));
        DurationHistogram.Record(duration.TotalMilliseconds);
    }

    public static void RecordRetry(int attempt, string reason)
    {
        RetryCounter.Add(1,
            new KeyValuePair<string, object?>("attempt", attempt),
            new KeyValuePair<string, object?>("reason", reason));
    }

    public static void RecordFailure(int attempts, TimeSpan duration)
    {
        FailureCounter.Add(1, new KeyValuePair<string, object?>("attempts", attempts));
        DurationHistogram.Record(duration.TotalMilliseconds);
    }
}
