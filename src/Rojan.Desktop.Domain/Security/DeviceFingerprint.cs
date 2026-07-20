namespace Rojan.Desktop.Domain.Security;

/// <summary>
/// Phase 25: Device Registration. A stable-but-recomputable hash of this
/// machine's identifying characteristics (machine name, OS description,
/// processor count - see <c>Infrastructure.Identity.DeviceRegistrationService</c>
/// for exactly what feeds it), recomputed on every launch and compared
/// against the persisted <see cref="Identity.DeviceIdentity.Fingerprint"/>
/// rather than trusted blindly - drift signals a hardware change without
/// invalidating the device's registered <see cref="Identity.DeviceIdentity.Id"/>.
/// </summary>
public sealed record DeviceFingerprint(string Value, DateTimeOffset ComputedAt);
