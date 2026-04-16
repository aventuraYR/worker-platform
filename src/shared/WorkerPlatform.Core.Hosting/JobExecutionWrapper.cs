using Microsoft.Extensions.Logging;
using WorkerPlatform.Core.Models;

namespace WorkerPlatform.Core.Middleware;

public class JobExecutionWrapper
{
    private readonly ILogger<JobExecutionWrapper> _logger;

    public JobExecutionWrapper(ILogger<JobExecutionWrapper> logger)
    {
        _logger = logger;
    }

    public async Task<JobResult> ExecuteAsync(
        string jobName,
        Func<CancellationToken, Task> job,
        CancellationToken ct)
    {
        var startTime   = DateTimeOffset.UtcNow;
        var executionId = Guid.NewGuid();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["JobName"]     = jobName,
            ["ExecutionId"] = executionId
        });

        _logger.LogInformation(
            "Job {JobName} iniciado. ExecutionId: {ExecutionId}",
            jobName, executionId);

        try
        {
            await job(ct);

            var duration = DateTimeOffset.UtcNow - startTime;

            _logger.LogInformation(
                "Job {JobName} completado en {DurationMs}ms. Exitoso: {IsSuccess}",
                jobName, duration.TotalMilliseconds, true);

            return JobResult.Success(jobName, duration);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Job {JobName} cancelado", jobName);
            return JobResult.Cancelled(jobName);
        }
        catch (Exception ex)
        {
            var duration = DateTimeOffset.UtcNow - startTime;

            _logger.LogError(ex,
                "Job {JobName} falló después de {DurationMs}ms. Exitoso: {IsSuccess}",
                jobName, duration.TotalMilliseconds, false);

            return JobResult.Failure(jobName, duration, ex);
        }
    }
}