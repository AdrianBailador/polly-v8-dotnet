using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace PollyV8Dotnet.Resilience;

/// <summary>
/// Same two strategies, opposite order. Retry-outer/Timeout-inner gives each attempt
/// its own timeout budget; Timeout-outer/Retry-inner puts one deadline over every
/// attempt and every delay between them combined. Durations are parameters so the
/// same pipeline shape can run with realistic values in production and tiny ones
/// in tests.
/// </summary>
public static class OrderingDemo
{
    public static ResiliencePipeline RetryOuterTimeoutInner(TimeProvider timeProvider, TimeSpan perAttemptTimeout, TimeSpan retryDelay) =>
        new ResiliencePipelineBuilder { TimeProvider = timeProvider }
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = retryDelay,
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = new PredicateBuilder().Handle<TimeoutRejectedException>(),
            })
            .AddTimeout(perAttemptTimeout)
            .Build();

    public static ResiliencePipeline TimeoutOuterRetryInner(TimeProvider timeProvider, TimeSpan overallTimeout, TimeSpan retryDelay, int maxRetryAttempts = 5) =>
        new ResiliencePipelineBuilder { TimeProvider = timeProvider }
            .AddTimeout(overallTimeout)
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetryAttempts,
                Delay = retryDelay,
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
            })
            .Build();
}
