namespace WorkerPlatform.Core.Models;

public record JobResult(
    string   JobName,
    bool     IsSuccess,
    TimeSpan Duration,
    string?  ErrorMessage = null,
    bool     IsCancelled  = false)
{
    public static JobResult Success(string name, TimeSpan duration) =>
        new(name, true, duration);

    public static JobResult Failure(string name, TimeSpan duration, Exception ex) =>
        new(name, false, duration, ex.Message);

    public static JobResult Cancelled(string name) =>
        new(name, false, TimeSpan.Zero, IsCancelled: true);

    public override string ToString() => IsSuccess
        ? $"{JobName} completado en {Duration.TotalMilliseconds:F0}ms"
        : $"{JobName} falló: {ErrorMessage}";
}