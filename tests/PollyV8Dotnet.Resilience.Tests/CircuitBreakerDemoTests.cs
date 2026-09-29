using Microsoft.Extensions.Time.Testing;
using Polly.CircuitBreaker;
using PollyV8Dotnet.Resilience;
using Xunit;

namespace PollyV8Dotnet.Resilience.Tests;

public class CircuitBreakerDemoTests
{
    [Fact]
    public async Task OpensAfterFailureThreshold()
    {
        var timeProvider = new FakeTimeProvider();
        var demo = new CircuitBreakerDemo(timeProvider);

        for (var i = 0; i < 4; i++) // MinimumThroughput
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                demo.CallAsync(_ => throw new InvalidOperationException("down")));
        }

        Assert.Equal(CircuitState.Open, demo.StateProvider.CircuitState);

        var callsAfterOpen = 0;
        await Assert.ThrowsAsync<BrokenCircuitException>(() =>
            demo.CallAsync(_ =>
            {
                callsAfterOpen++;
                return ValueTask.FromResult("unreachable");
            }));

        Assert.Equal(0, callsAfterOpen); // the open circuit never invoked the delegate
    }

    [Fact]
    public async Task RecoversToClosed_OnceBreakDurationHasPassed()
    {
        var timeProvider = new FakeTimeProvider();
        var demo = new CircuitBreakerDemo(timeProvider);

        for (var i = 0; i < 4; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                demo.CallAsync(_ => throw new InvalidOperationException("down")));
        }

        Assert.Equal(CircuitState.Open, demo.StateProvider.CircuitState);

        timeProvider.Advance(TimeSpan.FromSeconds(5)); // BreakDuration, no real waiting

        var result = await demo.CallAsync(_ => ValueTask.FromResult("ok"));

        Assert.Equal("ok", result);
        Assert.Equal(CircuitState.Closed, demo.StateProvider.CircuitState);
    }
}
