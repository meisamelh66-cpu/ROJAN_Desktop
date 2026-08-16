using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Infrastructure.Organizations;
using Rojan.Desktop.Presentation.Organizations;
using Rojan.Desktop.Shell.Organizations;

namespace Rojan.Desktop.Shell.Tests.Organizations;

/// <summary>
/// Exercises <see cref="CurrentSessionService"/> against a temp settings
/// file (never the real %LocalAppData%\RojanDesktop\session.json) via its
/// internal path-overriding constructor - same pattern as
/// <c>Theming.ThemeServiceTests</c>/<c>Localization.LocalizationServiceTests</c>.
/// Covers first-launch defaults, Branch Switching (live, event-driven, no
/// restart), and Organization Scoping (switching branch also re-scopes
/// <see cref="ICurrentSessionService.AvailableBranches"/> to the new
/// branch's own organization) - all via <see cref="_salonContextService"/>
/// resolving to no real membership (its default), so every test in this
/// group exercises the unchanged fake-organization fallback path exactly
/// as before Reception Production Integration. The real-membership path is
/// covered separately, in the "Reception Production Integration" region
/// below.
/// </summary>
public sealed class CurrentSessionServiceTests : IDisposable
{
    private readonly string _settingsFilePath;
    private readonly OrganizationQueryService _queryService;
    private readonly StubSalonContextService _salonContextService;

    public CurrentSessionServiceTests()
    {
        _settingsFilePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "session.json");
        _queryService = new OrganizationQueryService(new FakeOrganizationRepository());
        _salonContextService = new StubSalonContextService();
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task InitializeAsync_WithNoSettingsFile_DefaultsToFirstSeededOrganizationAndBranchAsPlatformOwner()
    {
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);

        await service.InitializeAsync();

        Assert.Equal("org-1", service.CurrentOrganization?.Id);
        Assert.Equal("branch-1", service.CurrentBranch?.Id);
        Assert.Equal(WorkspaceRole.PlatformOwner, service.CurrentRole);
    }

    [Fact]
    public async Task InitializeAsync_ScopesAvailableBranchesToTheCurrentOrganizationOnly()
    {
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);

        await service.InitializeAsync();

        Assert.Equal(2, service.AvailableBranches.Count);
        Assert.All(service.AvailableBranches, b => Assert.Equal("org-1", b.OrganizationId));
    }

    [Fact]
    public async Task SwitchBranchAsync_ToBranchInSameOrganization_UpdatesCurrentBranchLiveWithoutChangingOrganization()
    {
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);
        await service.InitializeAsync();
        var raised = false;
        service.SessionChanged += (_, _) => raised = true;

        await service.SwitchBranchAsync("branch-2");

        Assert.Equal("branch-2", service.CurrentBranch?.Id);
        Assert.Equal("org-1", service.CurrentOrganization?.Id);
        Assert.True(raised);
    }

    [Fact]
    public async Task SwitchBranchAsync_ToBranchInDifferentOrganization_ReScopesOrganizationAndAvailableBranches()
    {
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);
        await service.InitializeAsync();

        await service.SwitchBranchAsync("branch-3");

        Assert.Equal("org-2", service.CurrentOrganization?.Id);
        Assert.Equal("branch-3", service.CurrentBranch?.Id);
        Assert.Single(service.AvailableBranches);
        Assert.All(service.AvailableBranches, b => Assert.Equal("org-2", b.OrganizationId));
    }

    [Fact]
    public async Task SwitchBranchAsync_PersistsSelection_RestoredByNextInitializeAsync()
    {
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);
        await service.InitializeAsync();
        await service.SwitchBranchAsync("branch-2");

        var restarted = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);
        await restarted.InitializeAsync();

        Assert.Equal("branch-2", restarted.CurrentBranch?.Id);
        Assert.Equal("org-1", restarted.CurrentOrganization?.Id);
    }

    [Fact]
    public async Task SwitchRoleAsync_UpdatesCurrentRoleLiveAndRaisesSessionChanged()
    {
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);
        await service.InitializeAsync();
        var raised = false;
        service.SessionChanged += (_, _) => raised = true;

        await service.SwitchRoleAsync(WorkspaceRole.BranchManager);

        Assert.Equal(WorkspaceRole.BranchManager, service.CurrentRole);
        Assert.True(raised);
    }

    [Fact]
    public async Task SwitchBranchAsync_WithUnknownBranchId_Throws()
    {
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);
        await service.InitializeAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SwitchBranchAsync("branch-does-not-exist"));
    }

    [Fact]
    public async Task SwitchBranchAsync_PushesToRecentBranchIdsNewestFirstWithoutDuplicates()
    {
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);
        await service.InitializeAsync();

        await service.SwitchBranchAsync("branch-2");
        await service.SwitchBranchAsync("branch-3");
        await service.SwitchBranchAsync("branch-1");
        await service.SwitchBranchAsync("branch-2");

        Assert.Equal(["branch-2", "branch-1", "branch-3"], service.RecentBranchIds);
    }

    [Fact]
    public async Task SwitchBranchAsync_CapsRecentBranchIdsAtMax()
    {
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);
        await service.InitializeAsync();

        await service.SwitchBranchAsync("branch-1");
        await service.SwitchBranchAsync("branch-2");
        await service.SwitchBranchAsync("branch-3");
        await service.SwitchBranchAsync("branch-1");
        await service.SwitchBranchAsync("branch-2");
        await service.SwitchBranchAsync("branch-3");

        Assert.True(service.RecentBranchIds.Count <= CurrentSessionService.MaxRecentBranches);
    }

    [Fact]
    public async Task ToggleFavoriteBranchAsync_TogglesMembershipAndPersists()
    {
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);
        await service.InitializeAsync();
        var raised = false;
        service.SessionChanged += (_, _) => raised = true;

        await service.ToggleFavoriteBranchAsync("branch-2");
        Assert.Contains("branch-2", service.FavoriteBranchIds);
        Assert.True(raised);

        await service.ToggleFavoriteBranchAsync("branch-2");
        Assert.DoesNotContain("branch-2", service.FavoriteBranchIds);

        await service.ToggleFavoriteBranchAsync("branch-3");
        var restarted = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);
        await restarted.InitializeAsync();
        Assert.Contains("branch-3", restarted.FavoriteBranchIds);
    }

    // ---- Reception Production Integration: real membership resolution ----

    [Fact]
    public async Task InitializeAsync_OwnerSalonContext_ResolvesToOrganizationOwnerAndSkipsTheFakeOrganizationPath()
    {
        _salonContextService.Context = new SalonContext("salon-1", "Glow Salon", IsOwner: true, MembershipRole: null);
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);

        await service.InitializeAsync();

        Assert.Equal("salon-1", service.CurrentOrganization?.Id);
        Assert.Equal("Glow Salon", service.CurrentOrganization?.Name);
        Assert.Equal(WorkspaceRole.OrganizationOwner, service.CurrentRole);
        Assert.Null(service.CurrentBranch);
    }

    [Fact]
    public async Task InitializeAsync_AcceptedReceptionistInvite_ResolvesToReceptionRole()
    {
        _salonContextService.Context = new SalonContext("salon-9", "Glow Salon", IsOwner: false, MembershipRole: "RECEPTIONIST");
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);

        await service.InitializeAsync();

        Assert.Equal(WorkspaceRole.Reception, service.CurrentRole);
        Assert.Equal("salon-9", service.CurrentOrganization?.Id);
    }

    [Fact]
    public async Task InitializeAsync_AcceptedManagerInvite_ResolvesToOrganizationManagerRole()
    {
        _salonContextService.Context = new SalonContext("salon-9", "Glow Salon", IsOwner: false, MembershipRole: "MANAGER");
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);

        await service.InitializeAsync();

        Assert.Equal(WorkspaceRole.OrganizationManager, service.CurrentRole);
    }

    [Fact]
    public async Task InitializeAsync_NoRealMembership_FallsBackToTheExistingFakeOrganizationPathUnchanged()
    {
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);

        await service.InitializeAsync();

        Assert.Equal("org-1", service.CurrentOrganization?.Id);
        Assert.Equal(WorkspaceRole.PlatformOwner, service.CurrentRole);
    }

    [Fact]
    public async Task InitializeAsync_RealMembershipResolved_HasRealMembershipIsTrue()
    {
        _salonContextService.Context = new SalonContext("salon-9", "Glow Salon", IsOwner: false, MembershipRole: "RECEPTIONIST");
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);

        await service.InitializeAsync();

        Assert.True(service.HasRealMembership);
    }

    [Fact]
    public async Task InitializeAsync_NoRealMembership_HasRealMembershipIsFalse()
    {
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);

        await service.InitializeAsync();

        Assert.False(service.HasRealMembership);
    }

    [Fact]
    public async Task SwitchRoleAsync_SessionHasRealMembership_Throws()
    {
        _salonContextService.Context = new SalonContext("salon-9", "Glow Salon", IsOwner: false, MembershipRole: "RECEPTIONIST");
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);
        await service.InitializeAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SwitchRoleAsync(WorkspaceRole.PlatformOwner));
        Assert.Equal(WorkspaceRole.Reception, service.CurrentRole);
    }

    [Fact]
    public async Task SwitchBranchAsync_SessionHasRealMembership_Throws()
    {
        _salonContextService.Context = new SalonContext("salon-9", "Glow Salon", IsOwner: false, MembershipRole: "RECEPTIONIST");
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);
        await service.InitializeAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SwitchBranchAsync("branch-1"));
    }

    [Fact]
    public async Task SwitchRoleAsync_NoRealMembership_StillWorksUnchanged()
    {
        // Confirms the guard is scoped to real sessions only - every out-of-scope fake module's dev/demo experience is untouched.
        var service = new CurrentSessionService(_queryService, _salonContextService, _settingsFilePath);
        await service.InitializeAsync();

        await service.SwitchRoleAsync(WorkspaceRole.Inventory);

        Assert.Equal(WorkspaceRole.Inventory, service.CurrentRole);
    }

    private sealed class StubSalonContextService : ISalonContextService
    {
        public SalonContext? Context { get; set; }

        public Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(Context?.SalonId);

        public Task<SalonContext?> GetCurrentContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(Context);
    }
}
