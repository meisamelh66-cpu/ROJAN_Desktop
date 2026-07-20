using Rojan.Desktop.Domain.Identity;

namespace Rojan.Desktop.Application.Identity;

/// <summary>
/// Phase 25: Device Registration. Owns this installation's
/// <see cref="DeviceIdentity"/>/<see cref="InstallationIdentity"/> -
/// generated once on first run, persisted, and returned unchanged on
/// every subsequent call (the concrete implementation recomputes the
/// device fingerprint each call to detect hardware drift, but never
/// mints a new <see cref="DeviceIdentity.Id"/> for an already-registered
/// device). <see cref="EnsureRegisteredAsync"/> is idempotent -
/// safe to call at every startup, same shape as
/// <c>ICurrentSessionService.InitializeAsync</c>.
/// </summary>
public interface IDeviceRegistrationService
{
    /// <summary>Null until <see cref="EnsureRegisteredAsync"/> has completed at least once.</summary>
    public DeviceIdentity? CurrentDevice { get; }

    public InstallationIdentity? CurrentInstallation { get; }

    /// <summary>Registers this device/installation if not already registered, otherwise refreshes the fingerprint and returns the existing identity unchanged.</summary>
    public Task<DeviceIdentity> EnsureRegisteredAsync(CancellationToken cancellationToken = default);
}
