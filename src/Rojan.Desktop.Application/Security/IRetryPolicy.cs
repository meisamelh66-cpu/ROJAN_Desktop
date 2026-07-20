namespace Rojan.Desktop.Application.Security;

/// <summary>
/// Phase 25: Secure API Foundation / Hybrid Offline/Online Platform. One
/// shared retry algorithm (exponential backoff with jitter - see
/// <see cref="RetryPolicy"/>) used by both <c>Infrastructure.Api.HttpApiClient</c>
/// and <c>Infrastructure.Sync.SyncQueueService</c>, so retry timing never
/// drifts between the two call sites that need it. Pure timing/control-
/// flow logic with no I/O of its own, which is why the default
/// implementation lives here in Application rather than Infrastructure -
/// same reasoning <c>PermissionEngine</c>/<c>PermissionGate</c> already
/// establishes for infrastructure-free logic.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>Runs <paramref name="operation"/>, retrying on any exception up to the policy's attempt limit with backoff between attempts. Rethrows the last exception if every attempt fails. Never retries once <paramref name="cancellationToken"/> is cancelled.</summary>
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
