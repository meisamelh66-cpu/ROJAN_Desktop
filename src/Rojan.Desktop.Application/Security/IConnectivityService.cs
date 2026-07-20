using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Application.Security;

/// <summary>Phase 25: Hybrid Offline/Online Platform. Observes backend reachability so <see cref="ISyncQueueService"/>/<see cref="Api.IApiClient"/> can decide whether to attempt a call at all rather than waiting on one that is going to fail.</summary>
public interface IConnectivityService : IDisposable
{
    public ConnectionState CurrentState { get; }

    /// <summary>Forces an immediate re-check rather than waiting for the next passive network-change notification.</summary>
    public Task<ConnectionState> CheckAsync(CancellationToken cancellationToken = default);

    public event EventHandler<ConnectionState>? StateChanged;
}
