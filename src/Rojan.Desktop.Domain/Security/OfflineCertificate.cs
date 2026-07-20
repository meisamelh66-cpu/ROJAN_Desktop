namespace Rojan.Desktop.Domain.Security;

/// <summary>
/// Phase 25: Offline Certificate Foundation. A locally-issued proof of
/// this installation's identity, usable while disconnected from any
/// backend - not a real X.509/PKI certificate (there is no Certificate
/// Authority yet), but modeled with the same shape one would have
/// (subject, thumbprint, validity window) so a future real CA-issued
/// certificate can replace <see cref="Thumbprint"/>'s generation without
/// changing this type or anything that consumes
/// <see cref="CertificateState"/>. Explicitly not a commercial license:
/// nothing in this app reads <see cref="CertificateState"/> to gate a
/// feature (Phase 25's scope forbids that).
/// </summary>
public sealed record OfflineCertificate(
    string SubjectId,
    string Thumbprint,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);
