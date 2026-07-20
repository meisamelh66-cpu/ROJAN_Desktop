using System.Net.NetworkInformation;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Infrastructure.Connectivity;

/// <summary>
/// Default <see cref="IConnectivityService"/>. Uses
/// <see cref="NetworkInterface.GetIsNetworkAvailable"/> for a real (if
/// coarse - "is any network adapter up," not "can the backend actually
/// be reached") reachability signal, and subscribes to
/// <see cref="NetworkChange.NetworkAvailabilityChanged"/> for passive
/// updates rather than polling. This deliberately stops short of an
/// actual reachability probe against a backend host, since Phase 25 has
/// no backend endpoint to probe yet - a future phase wiring a real API
/// base address can upgrade <see cref="CheckAsync"/> to also attempt a
/// lightweight HEAD/ping request without changing this interface.
/// </summary>
public sealed class ConnectivityService : IConnectivityService
{
    public ConnectivityService()
    {
        CurrentState = NetworkInterface.GetIsNetworkAvailable() ? ConnectionState.Online : ConnectionState.Offline;
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
    }

    public ConnectionState CurrentState { get; private set; }

    public event EventHandler<ConnectionState>? StateChanged;

    public Task<ConnectionState> CheckAsync(CancellationToken cancellationToken = default)
    {
        SetState(NetworkInterface.GetIsNetworkAvailable() ? ConnectionState.Online : ConnectionState.Offline);
        return Task.FromResult(CurrentState);
    }

    public void Dispose() => NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e) =>
        SetState(e.IsAvailable ? ConnectionState.Online : ConnectionState.Offline);

    private void SetState(ConnectionState state)
    {
        if (CurrentState == state)
        {
            return;
        }

        CurrentState = state;
        StateChanged?.Invoke(this, state);
    }
}
