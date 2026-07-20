using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Application.Security;

/// <summary>
/// Phase 25: Offline Certificate Foundation. Architecture only - issues,
/// validates, and renews this installation's <see cref="OfflineCertificate"/>.
/// Nothing in this app calls <see cref="CurrentState"/> to gate a feature
/// (explicitly out of scope this phase); the only consumer today is
/// diagnostics/future-sync-handshake code.
/// </summary>
public interface ICertificateService
{
    public OfflineCertificate? CurrentCertificate { get; }

    public CertificateState CurrentState { get; }

    public Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Issues a new certificate for this installation. Safe to call when one already exists - replaces it (equivalent to <see cref="RenewAsync"/>).</summary>
    public Task<OfflineCertificate> IssueAsync(CancellationToken cancellationToken = default);

    /// <summary>Extends the current certificate's validity window. Throws <see cref="InvalidOperationException"/> if none has been issued yet.</summary>
    public Task<OfflineCertificate> RenewAsync(CancellationToken cancellationToken = default);

    /// <summary>Re-derives <see cref="CurrentState"/> from <see cref="CurrentCertificate"/> against the current time - a cheap, side-effect-free re-check (no network/backend call), matching this phase's "offline verification" requirement.</summary>
    public CertificateState Validate();
}
