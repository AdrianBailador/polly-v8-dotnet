using Microsoft.Extensions.Time.Testing;
using PollyV8Dotnet.Resilience;
using Xunit;

namespace PollyV8Dotnet.Resilience.Tests;

public class RetryDemoTests
{
    [Fact]
    public async Task RetriesUntilSuccess_WithoutWaitingForRealDelays()
    {
        var timeProvider = new FakeTimeProvider();
        var demo = new RetryDemo(timeProvider);
        var attempts = 0;

        var callTask = demo.CallAsync(_ =>
        {
            attempts++;
            if (attempts < 3)
                throw new InvalidOperationException("flaky");
            return ValueTask.FromResult("ok");
        });

        // Two retries at 1s each. Yield so the pipeline's continuation actually
        // schedules its delay timer before we fast-forward past it.
        for (var i = 0; i < 2; i++)
        {
            await Task.Delay(10);
            timeProvider.Advance(TimeSpan.FromSeconds(1));
        }

        var result = await callTask;

        Assert.Equal("ok", result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task GivesUpAfterMaxRetryAttempts()
    {
        var timeProvider = new FakeTimeProvider();
        var demo = new RetryDemo(timeProvider);
        var attempts = 0;

        var callTask = demo.CallAsync(_ =>
        {
            attempts++;
            throw new InvalidOperationException("always fails");
        });

        for (var i = 0; i < 2; i++)
        {
            await Task.Delay(10);
            timeProvider.Advance(TimeSpan.FromSeconds(1));
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => callTask);
        Assert.Equal(3, attempts); // the initial attempt plus 2 retries
    }
}
