namespace Rojan.Desktop.Domain.Security;

/// <summary>Phase 25: Hybrid Offline/Online Platform. Network reachability of the backend, as observed by <c>Infrastructure.Connectivity.ConnectivityService</c>. <see cref="Degraded"/> covers "technically reachable but unreliable" (e.g. repeated timeouts) distinctly from a clean <see cref="Offline"/>, since the sync queue treats them differently (Degraded still attempts sync with backoff; Offline queues only).</summary>
public enum ConnectionState
{
    Offline,
    Connecting,
    Online,
    Degraded,
}
