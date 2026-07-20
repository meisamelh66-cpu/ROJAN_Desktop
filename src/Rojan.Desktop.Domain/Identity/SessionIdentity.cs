namespace Rojan.Desktop.Domain.Identity;

/// <summary>
/// Phase 25: Secure Authentication Foundation. The identity of one signed-
/// in session - a <see cref="UserIdentity"/> operating from a
/// <see cref="DeviceIdentity"/>, bounded by <see cref="ExpiresAt"/>.
/// Expiration is expressed as data here (a plain timestamp comparison,
/// see <see cref="Security.SessionRules"/>) rather than a method on this
/// record, keeping <see cref="SessionIdentity"/> itself a pure immutable
/// value with no notion of "now."
/// </summary>
public sealed record SessionIdentity(
    string Id,
    string UserId,
    string DeviceId,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);
