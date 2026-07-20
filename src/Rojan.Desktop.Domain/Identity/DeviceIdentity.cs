namespace Rojan.Desktop.Domain.Identity;

/// <summary>
/// Phase 25: Device Registration. The immutable identity of this physical
/// installation's device, as recorded the first time
/// <c>Infrastructure.Identity.DeviceRegistrationService</c> runs and
/// persisted thereafter (a device's <see cref="Id"/> never changes across
/// app restarts - only <see cref="Fingerprint"/> is recomputed each
/// launch, so a fingerprint drift from hardware changes can be detected
/// without treating it as a brand-new device). <see cref="PublicKey"/> is
/// the abstraction point Phase 25.2 asks for future asymmetric
/// registration/attestation to slot into - null until a real key pair is
/// provisioned, never a placeholder value.
/// </summary>
public sealed record DeviceIdentity(
    string Id,
    string Fingerprint,
    string MachineName,
    string OperatingSystemDescription,
    DateTimeOffset RegisteredAt,
    string? PublicKey = null);
