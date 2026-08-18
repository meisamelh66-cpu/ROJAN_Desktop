using Rojan.Desktop.Application.Identity;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Identity;

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
            Role: DesktopRoleBridge.ToDomainRole(_enterpriseContext.CurrentRole));

        var user = UserIdentity.LocalUser(Environment.UserName);

        return new EnterpriseIdentitySnapshot(
            workspace,
            user,
            device,
            _deviceRegistrationService.CurrentInstallation,
            _sessionService.CurrentSession);
    }
}
