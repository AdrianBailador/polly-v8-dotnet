using Polly;
using Polly.Retry;

namespace PollyV8Dotnet.Resilience;

/// <summary>
/// A retry pipeline wired to an injected <see cref="TimeProvider"/> instead of the
/// real clock Polly would otherwise use internally for its delays.
/// </summary>
public sealed class RetryDemo
{
    private readonly ResiliencePipeline _pipeline;

    public RetryDemo(TimeProvider timeProvider)
    {
        _pipeline = new ResiliencePipelineBuilder
        {
            TimeProvider = timeProvider,
        }
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Constant,
        })
        .Build();
    }

    public Task<string> CallAsync(Func<CancellationToken, ValueTask<string>> operation, CancellationToken cancellationToken = default) =>
        _pipeline.ExecuteAsync(operation, cancellationToken).AsTask();
}
