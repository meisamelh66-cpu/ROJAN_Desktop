using Rojan.Desktop.Application.Api.Contracts;

namespace Rojan.Desktop.Application.Identity;

/// <summary>
/// Phase B: Windows Reception Device Registration. Tells ROJAN_Backend
/// about this installation's already-existing <see cref="Domain.Identity.DeviceIdentity"/>/
/// <see cref="Domain.Identity.InstallationIdentity"/> (see
/// <see cref="IDeviceRegistrationService"/> - this service never generates
/// or reinterprets either, only reports them), scoped to exactly one
/// salon the caller has already been authorized for. Deliberately narrow -
/// one method, no revoke/list/heartbeat-scheduler surface (out of scope for
/// this phase, see the concrete implementation's own doc comment).
/// </summary>
public interface IDeviceAuthorizationService
{
    /// <summary>
    /// Registers (or, for an already-registered, non-revoked identity,
    /// heartbeats) this device for <paramref name="salonId"/> -
    /// <paramref name="salonId"/> must already be the caller's real,
    /// resolved active salon (see <see cref="Salons.ISalonContextService"/>),
    /// never an arbitrary/unvalidated value; the backend re-verifies it
    /// regardless. Never throws for an ordinary failure (authorization
    /// denial, validation rejection, network/connectivity trouble) - see
    /// <see cref="DeviceRegistrationResult"/>'s own doc comment for how
    /// each is reported instead.
    /// </summary>
    public Task<DeviceRegistrationResult> RegisterAsync(string salonId, CancellationToken cancellationToken = default);
}

/// <summary>See <see cref="IDeviceAuthorizationService.RegisterAsync"/>. Exactly one of <see cref="Registration"/>/<see cref="ErrorMessage"/> is non-null, depending on <see cref="Outcome"/>.</summary>
public sealed record DeviceRegistrationResult(DeviceRegistrationOutcome Outcome, DeviceRegistrationResponse? Registration, string? ErrorMessage)
{
    public static DeviceRegistrationResult Registered(DeviceRegistrationResponse registration) =>
        new(DeviceRegistrationOutcome.Registered, registration, null);

    public static DeviceRegistrationResult AuthorizationDenied(string? errorMessage) =>
        new(DeviceRegistrationOutcome.AuthorizationDenied, null, errorMessage);

    public static DeviceRegistrationResult Rejected(string? errorMessage) =>
        new(DeviceRegistrationOutcome.Rejected, null, errorMessage);

    public static DeviceRegistrationResult NetworkUnavailable(string? errorMessage) =>
        new(DeviceRegistrationOutcome.NetworkUnavailable, null, errorMessage);
}

/// <summary>
/// - <see cref="Registered"/>: the backend accepted the registration (a fresh row, or an idempotent
///   heartbeat on an existing, non-revoked one) - safe to persist local registration state.
/// - <see cref="AuthorizationDenied"/>: the backend rejected with 401/403 - the caller does not
///   actually control <c>salonId</c>, or the session itself is no longer valid. Must never be
///   treated as a successful registration.
/// - <see cref="Rejected"/>: any other non-2xx response (e.g. a validation failure) - a real backend
///   answer, just not success. Must never be treated as a successful registration.
/// - <see cref="NetworkUnavailable"/>: connectivity/timeout - transient, not a backend decision at
///   all. Safe to retry later with the exact same device identity; must never be treated as a
///   successful registration either.
/// </summary>
public enum DeviceRegistrationOutcome
{
    Registered,
    AuthorizationDenied,
    Rejected,
    NetworkUnavailable,
}
