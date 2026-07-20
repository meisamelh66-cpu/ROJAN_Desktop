using Rojan.Desktop.Application.Security;

namespace Rojan.Desktop.Application.Tests.Security;

public sealed class RetryPolicyTests
{
    [Fact]
    public async Task ExecuteAsync_SucceedsOnFirstAttempt_ReturnsResultWithoutRetrying()
    {
        var policy = new RetryPolicy();
        var attempts = 0;

        var result = await policy.ExecuteAsync(_ =>
        {
            attempts++;
            return Task.FromResult(42);
        });

        Assert.Equal(42, result);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_FailsThenSucceeds_RetriesUntilSuccess()
    {
        var policy = new RetryPolicy();
        var attempts = 0;

        var result = await policy.ExecuteAsync(_ =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new InvalidOperationException("transient failure");
            }

            return Task.FromResult("done");
        });

        Assert.Equal("done", result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_AlwaysFails_ThrowsAfterMaxAttemptsAndStopsRetrying()
    {
        var policy = new RetryPolicy();
        var attempts = 0;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => policy.ExecuteAsync<int>(_ =>
        {
            attempts++;
            throw new InvalidOperationException("permanent failure");
        }));

        Assert.Equal(RetryPolicy.MaxAttempts, attempts);
        Assert.Equal("permanent failure", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledBeforeFirstAttempt_ThrowsOperationCanceledExceptionWithoutInvokingOperation()
    {
        var policy = new RetryPolicy();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var invoked = false;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => policy.ExecuteAsync<int>(_ =>
        {
            invoked = true;
            return Task.FromResult(0);
        }, cts.Token));

        Assert.False(invoked);
    }
}
