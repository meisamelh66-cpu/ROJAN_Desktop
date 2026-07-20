using Rojan.Desktop.Domain.Organizations;

namespace Rojan.Desktop.Domain.Identity;

/// <summary>
/// Phase 25: Enterprise Identity Foundation. The immutable identity of the
/// dedicated workspace a session is currently operating as - an
/// organization/branch pair plus the <see cref="WorkspaceRole"/> already
/// modeled by <c>Domain.Organizations</c> (Phase 22). <see cref="BranchId"/>
/// is nullable because some roles (e.g. <see cref="WorkspaceRole.PlatformOwner"/>)
/// legitimately operate above any single branch.
/// </summary>
public sealed record WorkspaceIdentity(string OrganizationId, string? BranchId, WorkspaceRole Role);
