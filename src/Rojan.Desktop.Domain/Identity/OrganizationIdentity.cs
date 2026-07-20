namespace Rojan.Desktop.Domain.Identity;

/// <summary>
/// Phase 25: Enterprise Identity Foundation. The immutable identity of an
/// <see cref="Organizations.Organization"/> as far as the security/sync
/// platform is concerned - just enough to name and address a tenant
/// (never the full aggregate, which stays owned by
/// <c>Domain.Organizations</c>). Deliberately minimal: this bounded
/// context only needs to say "which organization," not carry billing/
/// subscription/branch-list state, so it does not duplicate
/// <see cref="Organizations.Organization"/>'s richer shape.
/// </summary>
public sealed record OrganizationIdentity(string Id, string Name);
