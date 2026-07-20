namespace Rojan.Desktop.Domain.Security;

/// <summary>
/// Phase 25: Offline Certificate Foundation. Validity stage of the
/// installation's <see cref="OfflineCertificate"/>, as computed by
/// <see cref="CertificateRules.DetermineState"/>. This is a pure
/// validity/lifecycle signal, not a commercial entitlement check - per
/// Phase 25's explicit scope, nothing in this app gates a feature on
/// <see cref="CertificateState"/> yet.
/// </summary>
public enum CertificateState
{
    NotIssued,
    Valid,
    ExpiringSoon,
    Expired,
    Revoked,
}
