using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Serilog;

namespace WorkerPlatform.Core.Resilience;

public static class ResiliencePolicies
{
    public static IServiceCollection AddSharedResilience(
        this IServiceCollection services)
    {
        services.AddResiliencePipeline("database", pipeline =>
        {
            pipeline
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay            = TimeSpan.FromSeconds(2),
                    BackoffType      = DelayBackoffType.Exponential,
                    OnRetry          = args =>
                    {
                        Log.Warning(
                            "Reintento {Attempt} de conexión a BD. Error: {Error}",
                            args.AttemptNumber,
                            args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    }
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio      = 0.5,
                    MinimumThroughput = 5,
                    BreakDuration     = TimeSpan.FromSeconds(30),
                    OnOpened          = args =>
                    {
                        Log.Error(
                            "Circuit breaker abierto. BD no disponible por {Duration}s",
                            args.BreakDuration.TotalSeconds);
                        return ValueTask.CompletedTask;
                    }
                })
                .AddTimeout(TimeSpan.FromSeconds(30));
        });

        return services;
    }
}