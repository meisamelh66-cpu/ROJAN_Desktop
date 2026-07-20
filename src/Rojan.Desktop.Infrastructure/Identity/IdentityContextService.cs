using Rojan.Desktop.Application.Identity;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Identity;
using DomainWorkspaceRole = Rojan.Desktop.Domain.Organizations.WorkspaceRole;

namespace Rojan.Desktop.Infrastructure.Identity;

/// <summary>
/// Default <see cref="IIdentityContextService"/>. Composes
/// <see cref="EnterpriseIdentitySnapshot"/> from three already-independent
/// sources at call time: <see cref="IEnterpriseContext"/> (organization/
/// branch/role), <see cref="IDeviceRegistrationService"/> (device/
/// installation), and <see cref="ISessionService"/> (the current session,
/// if any) - this class owns no state of its own beyond the local-user
/// bridge (<see cref="UserIdentity.LocalUser"/>), it only reads.
/// </summary>
public sealed class IdentityContextService : IIdentityContextService
{
    private readonly IEnterpriseContext _enterpriseContext;
    private readonly IDeviceRegistrationService _deviceRegistrationService;
    private readonly ISessionService _sessionService;

    public IdentityContextService(
        IEnterpriseContext enterpriseContext,
        IDeviceRegistrationService deviceRegistrationService,
        ISessionService sessionService)
    {
        _enterpriseContext = enterpriseContext;
        _deviceRegistrationService = deviceRegistrationService;
        _sessionService = sessionService;
    }

    public async Task<EnterpriseIdentitySnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var device = _deviceRegistrationService.CurrentDevice
            ?? await _deviceRegistrationService.EnsureRegisteredAsync(cancellationToken).ConfigureAwait(false);

        var workspace = new WorkspaceIdentity(
            OrganizationId: _enterpriseContext.CurrentOrganizationId ?? string.Empty,
            BranchId: _enterpriseContext.CurrentBranchId,
            Role: ToDomainRole(_enterpriseContext.CurrentRole));

        var user = UserIdentity.LocalUser(Environment.UserName);

        return new EnterpriseIdentitySnapshot(
            workspace,
            user,
            device,
            _deviceRegistrationService.CurrentInstallation,
            _sessionService.CurrentSession);
    }

    /// <summary>
    /// <see cref="IEnterpriseContext"/> deliberately carries Application's
    /// own <c>WorkspaceRole</c> copy (see that type's own doc comment),
    /// not Domain's - both enumerate the identical set of roles by
    /// design, so a name-based parse is the correct, tautology-free
    /// bridge rather than a hand-maintained switch that would silently
    /// drift if either enum ever gained a member the other didn't.
    /// </summary>
    private static DomainWorkspaceRole ToDomainRole(WorkspaceRole role) => Enum.Parse<DomainWorkspaceRole>(role.ToString());
}
