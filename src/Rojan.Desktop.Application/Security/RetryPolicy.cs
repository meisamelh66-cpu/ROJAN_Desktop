namespace Rojan.Desktop.Application.Security;

/// <summary>Default <see cref="IRetryPolicy"/>: exponential backoff with jitter, capped at <see cref="MaxAttempts"/> total attempts (the first try plus retries). Delay before attempt <c>n</c> (1-indexed, n &gt; 1) is <c>BaseDelay * 2^(n-2)</c> plus up to 100ms of jitter, so concurrent callers retrying after the same failure do not all retry in lockstep.</summary>
public sealed class RetryPolicy : IRetryPolicy
{
    /// <summary>First attempt plus up to 4 retries - enough to ride out a brief network blip without a caller waiting minutes for a queue-processing pass to give up.</summary>
    public const int MaxAttempts = 5;

    private static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(500);

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                lastException = exception;

                if (attempt == MaxAttempts)
                {
                    break;
                }

                var delay = TimeSpan.FromMilliseconds((BaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1)) + Random.Shared.Next(0, 100));
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? new InvalidOperationException("Retry policy exhausted without capturing an exception.");
    }
}
