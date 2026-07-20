namespace Rojan.Desktop.Domain.Identity;

/// <summary>
/// Phase 25: Device Registration. The immutable identity of this specific
/// install of the application - distinct from <see cref="DeviceIdentity"/>
/// (the physical machine can host more than one installation over time,
/// e.g. a reinstall after an OS refresh gets a new <see cref="Id"/> even
/// though <see cref="DeviceIdentity.Fingerprint"/> is unchanged).
/// </summary>
public sealed record InstallationIdentity(string Id, string AppVersion, DateTimeOffset InstalledAt);
