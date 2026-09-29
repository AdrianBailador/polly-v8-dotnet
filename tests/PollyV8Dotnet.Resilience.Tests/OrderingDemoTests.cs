using Polly.Timeout;
using PollyV8Dotnet.Resilience;
using Xunit;

namespace PollyV8Dotnet.Resilience.Tests;

/// <summary>
/// These two tests use small *real* delays instead of FakeTimeProvider. A timeout
/// racing an in-flight operation is exactly the kind of interaction that's hard to
/// fake convincingly (see the article for why); the delays here are small enough
/// (tens of milliseconds) that both tests still run in well under a second.
/// </summary>
public class OrderingDemoTests
{
    [Fact]
    public async Task RetryOuterTimeoutInner_GivesEachAttemptItsOwnBudget()
    {
        var perAttemptTimeout = TimeSpan.FromMilliseconds(30);
        var retryDelay = TimeSpan.FromMilliseconds(10);
        var pipeline = OrderingDemo.RetryOuterTimeoutInner(TimeProvider.System, perAttemptTimeout, retryDelay);
        var attempts = 0;

        var exception = await Assert.ThrowsAsync<TimeoutRejectedException>(() =>
            pipeline.ExecuteAsync(async ct =>
            {
                attempts++;
                await Task.Delay(TimeSpan.FromMilliseconds(60), ct); // always longer than the 30ms per-attempt timeout
                return "unreachable";
            }).AsTask());

        Assert.NotNull(exception);
        Assert.Equal(3, attempts); // initial attempt + 2 retries, each timing out independently
    }

    [Fact]
    public async Task TimeoutOuterRetryInner_CutsOffTheWholeRetrySequence()
    {
        var overallTimeout = TimeSpan.FromMilliseconds(70);
        var retryDelay = TimeSpan.FromMilliseconds(30);
        var pipeline = OrderingDemo.TimeoutOuterRetryInner(TimeProvider.System, overallTimeout, retryDelay, maxRetryAttempts: 5);
        var attempts = 0;

        var exception = await Record.ExceptionAsync(() =>
            pipeline.ExecuteAsync(async ct =>
            {
                attempts++;
                throw new InvalidOperationException("always fails");
            }).AsTask());

        // Exhausting all 5 retries at 30ms apart takes ~150ms on its own. The 70ms
        // outer deadline should cut the sequence off well before that, around the
        // second or third attempt.
        Assert.IsType<TimeoutRejectedException>(exception);
        Assert.True(attempts < 6, $"expected the outer timeout to cut retries short of exhausting all 6 possible attempts, but {attempts} attempts completed");
    }
}
