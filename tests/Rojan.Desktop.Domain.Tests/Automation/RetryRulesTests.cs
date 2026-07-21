using Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Domain.Tests.Automation;

/// <summary>Exercises <see cref="RetryRules"/>'s pure retry/backoff arithmetic.</summary>
public sealed class RetryRulesTests
{
    [Theory]
    [InlineData(1, 3, true)]
    [InlineData(3, 3, true)]
    [InlineData(4, 3, false)]
    public void ShouldRetry_ComparesAttemptAgainstMaxRetries(int attemptNumber, int maxRetries, bool expected)
    {
        var policy = new RetryPolicy(maxRetries, RetryDelaySeconds: 1, TimeoutSeconds: 30);

        Assert.Equal(expected, RetryRules.ShouldRetry(attemptNumber, policy));
    }

    [Fact]
    public void ComputeBackoffDelaySeconds_ZeroDelay_AlwaysReturnsZero()
    {
        var policy = new RetryPolicy(MaxRetries: 3, RetryDelaySeconds: 0, TimeoutSeconds: 30);

        Assert.Equal(0, RetryRules.ComputeBackoffDelaySeconds(5, policy));
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(3, 20)]
    public void ComputeBackoffDelaySeconds_DoublesEachAttempt(int attemptNumber, int expectedSeconds)
    {
        var policy = new RetryPolicy(MaxRetries: 5, RetryDelaySeconds: 5, TimeoutSeconds: 30);

        Assert.Equal(expectedSeconds, RetryRules.ComputeBackoffDelaySeconds(attemptNumber, policy));
    }

    [Fact]
    public void None_HasNoRetriesAndA30SecondTimeout()
    {
        Assert.Equal(0, RetryPolicy.None.MaxRetries);
        Assert.Equal(30, RetryPolicy.None.TimeoutSeconds);
    }
}
