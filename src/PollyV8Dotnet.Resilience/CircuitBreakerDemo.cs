using Polly;
using Polly.CircuitBreaker;

namespace PollyV8Dotnet.Resilience;

/// <summary>
/// A circuit breaker over a dependency that fails hard. Exposes the state provider
/// so callers (and tests) can inspect Closed / Open / HalfOpen / Isolated directly,
/// instead of inferring state from which exception came back.
/// </summary>
public sealed class CircuitBreakerDemo
{
    private readonly ResiliencePipeline _pipeline;

    public CircuitBreakerStateProvider StateProvider { get; } = new();

    public CircuitBreakerDemo(TimeProvider timeProvider)
    {
        _pipeline = new ResiliencePipelineBuilder
        {
            TimeProvider = timeProvider,
        }
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 4,
            SamplingDuration = TimeSpan.FromSeconds(10),
            BreakDuration = TimeSpan.FromSeconds(5),
            ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
            StateProvider = StateProvider,
        })
        .Build();
    }

    public Task<string> CallAsync(Func<CancellationToken, ValueTask<string>> operation, CancellationToken cancellationToken = default) =>
        _pipeline.ExecuteAsync(operation, cancellationToken).AsTask();
}
