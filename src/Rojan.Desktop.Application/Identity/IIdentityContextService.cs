namespace Rojan.Desktop.Application.Identity;

/// <summary>Phase 25: Enterprise Identity Foundation. Composes the current <see cref="EnterpriseIdentitySnapshot"/> on demand - never cached by this interface itself (the concrete implementation may cache the parts that do not change within a session, e.g. device/installation), so every call reflects the current organization/branch/role/session.</summary>
public interface IIdentityContextService
{
    public Task<EnterpriseIdentitySnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
