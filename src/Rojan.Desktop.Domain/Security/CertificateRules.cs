namespace Rojan.Desktop.Domain.Security;

/// <summary>Phase 25: Offline Certificate Foundation. Pure derivation of <see cref="CertificateState"/> from an <see cref="OfflineCertificate"/> - mirrors <see cref="SessionRules"/>'s shape (a value type plus "now" in, a state enum out, no I/O).</summary>
public static class CertificateRules
{
    /// <summary>A certificate within this window of its expiry is still <see cref="CertificateState.Valid"/> but reports <see cref="CertificateState.ExpiringSoon"/> so a caller can renew proactively - 30 days, generous enough for an offline-first app that may not check in daily.</summary>
    public static readonly TimeSpan ExpiringSoonWindow = TimeSpan.FromDays(30);

    public static CertificateState DetermineState(OfflineCertificate? certificate, DateTimeOffset now)
    {
        if (certificate is null)
        {
            return CertificateState.NotIssued;
        }

        if (now >= certificate.ExpiresAt)
        {
            return CertificateState.Expired;
        }

        return certificate.ExpiresAt - now <= ExpiringSoonWindow
            ? CertificateState.ExpiringSoon
            : CertificateState.Valid;
    }
}
