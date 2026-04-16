using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WorkerPlatform.Core.Health;

internal static class HealthTelemetry
{
    private static readonly Meter Meter = new(
        "WorkerPlatform.Core.Health",
        "1.0.0");

    private static readonly Counter<long> ProbeRunsCounter = Meter.CreateCounter<long>(
        "workerplatform.health.probe.runs");

    private static readonly Counter<long> ProbeFailuresCounter = Meter.CreateCounter<long>(
        "workerplatform.health.probe.failures");

    private static readonly Histogram<double> ProbeDurationHistogram = Meter.CreateHistogram<double>(
        "workerplatform.health.probe.duration.ms");

    private static readonly ActivitySource ActivitySource = new(
        "WorkerPlatform.Core.Health.Probes");

    public static Activity? StartProbeActivity(string probe)
    {
        var activity = ActivitySource.StartActivity(
            "health.probe",
            ActivityKind.Internal);

        activity?.SetTag("health.probe", probe);
        return activity;
    }

    public static void RecordProbe(
        string probe,
        HealthStatus status,
        TimeSpan duration,
        int checksCount)
    {
        ProbeRunsCounter.Add(1,
            new KeyValuePair<string, object?>("probe", probe),
            new KeyValuePair<string, object?>("status", status.ToString()));

        if (status != HealthStatus.Healthy)
        {
            ProbeFailuresCounter.Add(1,
                new KeyValuePair<string, object?>("probe", probe),
                new KeyValuePair<string, object?>("status", status.ToString()));
        }

        ProbeDurationHistogram.Record(duration.TotalMilliseconds,
            new KeyValuePair<string, object?>("probe", probe),
            new KeyValuePair<string, object?>("checks", checksCount));
    }
}
