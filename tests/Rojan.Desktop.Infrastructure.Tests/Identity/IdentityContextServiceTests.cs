using Rojan.Desktop.Application.Identity;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;
using Rojan.Desktop.Infrastructure.Identity;
using DomainRole = Rojan.Desktop.Domain.Organizations.WorkspaceRole;

namespace Rojan.Desktop.Infrastructure.Tests.Identity;

/// <summary>
/// Phase 2A Role Bridge Cleanup: exercises <see cref="IdentityContextService.GetSnapshotAsync"/> -
/// this class had no direct test coverage before this phase (only the
/// interface declaration and the implementation itself referenced it
/// anywhere in the codebase). Confirms <see cref="Rojan.Desktop.Domain.Identity.WorkspaceIdentity.Role"/>
/// is populated via <see cref="DesktopRoleBridge.ToDomainRole"/>, matching
/// what the retired <c>IdentityContextService.ToDomainRole</c> would have
/// produced for the same input.
/// </summary>
public sealed class IdentityContextServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_PopulatesWorkspaceRole_ViaDesktopRoleBridge()
    {
        var enterpriseContext = new StubEnterpriseContext { CurrentRole = WorkspaceRole.OrganizationManager };
        var service = new IdentityContextService(enterpriseContext, new StubDeviceRegistrationService(), new StubSessionService());

        var snapshot = await service.GetSnapshotAsync();

        Assert.Equal(DesktopRoleBridge.ToDomainRole(WorkspaceRole.OrganizationManager), snapshot.Workspace.Role);
        Assert.Equal(DomainRole.OrganizationManager, snapshot.Workspace.Role);
    }

    [Fact]
    public async Task GetSnapshotAsync_CarriesOrganizationAndBranchIds()
    {
        var enterpriseContext = new StubEnterpriseContext { CurrentOrganizationId = "org-9", CurrentBranchId = "branch-3" };
        var service = new IdentityContextService(enterpriseContext, new StubDeviceRegistrationService(), new StubSessionService());

        var snapshot = await service.GetSnapshotAsync();

        Assert.Equal("org-9", snapshot.Workspace.OrganizationId);
        Assert.Equal("branch-3", snapshot.Workspace.BranchId);
    }

    [Fact]
    public async Task GetSnapshotAsync_NullOrganizationId_FallsBackToEmptyString()
    {
        var enterpriseContext = new StubEnterpriseContext { CurrentOrganizationId = null };
        var service = new IdentityContextService(enterpriseContext, new StubDeviceRegistrationService(), new StubSessionService());

        var snapshot = await service.GetSnapshotAsync();

        Assert.Equal(string.Empty, snapshot.Workspace.OrganizationId);
    }

    private sealed class StubEnterpriseContext : IEnterpriseContext
    {
        public string? CurrentOrganizationId { get; set; } = "org-1";

        public string? CurrentBranchId { get; set; } = "branch-1";

        public WorkspaceRole CurrentRole { get; set; } = WorkspaceRole.PlatformOwner;

        public IReadOnlySet<string> BackendPermissions { get; set; } = new HashSet<string>();
    }

    private sealed class StubDeviceRegistrationService : IDeviceRegistrationService
    {
        public DeviceIdentity? CurrentDevice { get; } = new("device-1", "fingerprint", "machine", "os", DateTimeOffset.UtcNow);

        public InstallationIdentity? CurrentInstallation { get; }

        public Task<DeviceIdentity> EnsureRegisteredAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Test double already has a non-null CurrentDevice - EnsureRegisteredAsync should never be called.");
    }

    private sealed class StubSessionService : ISessionService
    {
        public SessionIdentity? CurrentSession { get; }

        public AuthToken? CurrentAccessToken { get; }

        public AuthenticationState CurrentState { get; } = AuthenticationState.SignedOut;

        public Task InitializeAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("IdentityContextService never initializes the session service.");

        public Task<SessionIdentity> CreateSessionAsync(UserIdentity user, DeviceIdentity device, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("IdentityContextService never creates sessions.");

        public Task<SessionIdentity> CreateSessionFromTokensAsync(UserIdentity user, DeviceIdentity device, AuthToken accessToken, RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("IdentityContextService never creates sessions.");

        public Task<SessionIdentity> RefreshAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("IdentityContextService never refreshes sessions.");

        public Task ExpireAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("IdentityContextService never expires sessions.");

        public event EventHandler<AuthenticationState>? StateChanged
        {
            add { }
            remove { }
        }
    }
}
