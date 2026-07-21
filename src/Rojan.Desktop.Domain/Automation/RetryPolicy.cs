namespace Rojan.Desktop.Domain.Automation;

/// <summary>A step or workflow's Requirement 32.11 (Error Recovery) settings - how many times to retry, how long to wait between attempts, and the hard execution timeout.</summary>
public sealed record RetryPolicy(int MaxRetries, int RetryDelaySeconds, int TimeoutSeconds)
{
    /// <summary>No retries, a 30-second timeout - the default for any step that doesn't specify its own policy.</summary>
    public static RetryPolicy None { get; } = new(MaxRetries: 0, RetryDelaySeconds: 0, TimeoutSeconds: 30);
}

/// <summary>Pure retry/backoff arithmetic over a <see cref="RetryPolicy"/> - no I/O, no timers, so it's independently unit-testable from the engine that actually sleeps/retries (<c>Application.Automation.WorkflowExecutionEngine</c>).</summary>
public static class RetryRules
{
    /// <summary><paramref name="attemptNumber"/> is 1-based (the attempt that just failed). Retries while there are attempts left under <see cref="RetryPolicy.MaxRetries"/>.</summary>
    public static bool ShouldRetry(int attemptNumber, RetryPolicy policy) => attemptNumber <= policy.MaxRetries;

    /// <summary>Exponential backoff: <c>RetryDelaySeconds * 2^(attemptNumber - 1)</c>. A <see cref="RetryPolicy.RetryDelaySeconds"/> of 0 always yields 0 (no delay), regardless of attempt number.</summary>
    public static int ComputeBackoffDelaySeconds(int attemptNumber, RetryPolicy policy)
    {
        if (policy.RetryDelaySeconds <= 0)
        {
            return 0;
        }

        var multiplier = 1 << Math.Max(0, attemptNumber - 1);
        return policy.RetryDelaySeconds * multiplier;
    }
}
