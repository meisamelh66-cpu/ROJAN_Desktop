using Rojan.Desktop.Domain.Identity;

namespace Rojan.Desktop.Application.Identity;

/// <summary>
/// Phase 25: Enterprise Identity Foundation. Everything the future backend
/// sync/registration handshake needs to know about "who and what is
/// asking" in one immutable snapshot - composed by
/// <see cref="IIdentityContextService"/> from the existing
/// <c>IEnterpriseContext</c> (organization/branch/role) plus this phase's
/// new <see cref="DeviceIdentity"/>/<see cref="InstallationIdentity"/>/
/// <see cref="SessionIdentity"/>. Deliberately does not carry
/// <see cref="OrganizationIdentity"/>/<see cref="BranchIdentity"/>
/// (which need display names) - <see cref="Workspace"/> already carries
/// every id a sync payload needs, and resolving names would pull
/// <c>Application.Organizations.IOrganizationQueryService</c> into this
/// snapshot for no consumer this phase actually has.
/// </summary>
public sealed record EnterpriseIdentitySnapshot(
    WorkspaceIdentity Workspace,
    UserIdentity User,
    DeviceIdentity? Device,
    InstallationIdentity? Installation,
    SessionIdentity? Session);
