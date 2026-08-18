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
///
/// Phase 2B Context State Hardening: the seeded-organization fixture
/// (Branch Switching, Organization Scoping, persistence, favorites) is now
/// only reachable with <see cref="StubDemoModeProvider.IsEnabled"/> set -
/// these tests explicitly opt into Demo Context via <see cref="_demoModeProvider"/>
/// to keep exercising that fixture, same as before this phase, just no
/// longer reached silently. The real-membership path (owner/staff) is
/// covered separately, in the "Reception Production Integration" region;
/// the no-business-context path (the P0 fix this phase exists for) has its
/// own dedicated region below.
/// </summary>
public sealed class CurrentSessionServiceTests : IDisposable
{
    private readonly string _settingsFilePath;
    private readonly OrganizationQueryService _queryService;
    private readonly StubSalonContextService _salonContextService;
    private readonly ISalonSessionAdapter _salonSessionAdapter;
    private readonly StubDemoModeProvider _demoModeProvider;

    public CurrentSessionServiceTests()
    {
        _settingsFilePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "session.json");
        _queryService = new OrganizationQueryService(new FakeOrganizationRepository());
        _salonContextService = new StubSalonContextService();
        _salonSessionAdapter = new SalonSessionAdapter();
        _demoModeProvider = new StubDemoModeProvider { IsEnabled = true };
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private CurrentSessionService CreateService() =>
        new(_queryService, _salonContextService, _salonSessionAdapter, _demoModeProvider, _settingsFilePath);

    // ---- Demo Context: the seeded-organization fixture, now explicitly opted into ----

    [Fact]
    public async Task InitializeAsync_DemoModeEnabledWithNoSettingsFile_DefaultsToFirstSeededOrganizationAndBranchAsPlatformOwner()
    {
        var service = CreateService();

        await service.InitializeAsync();

        Assert.Equal("org-1", service.CurrentOrganization?.Id);
        Assert.Equal("branch-1", service.CurrentBranch?.Id);
        Assert.Equal(WorkspaceRole.PlatformOwner, service.CurrentRole);
        Assert.Equal(DesktopContextState.DemoContext, service.ContextState);
    }

    [Fact]
    public async Task InitializeAsync_ScopesAvailableBranchesToTheCurrentOrganizationOnly()
    {
        var service = CreateService();

        await service.InitializeAsync();

        Assert.Equal(2, service.AvailableBranches.Count);
        Assert.All(service.AvailableBranches, b => Assert.Equal("org-1", b.OrganizationId));
    }

    [Fact]
    public async Task SwitchBranchAsync_ToBranchInSameOrganization_UpdatesCurrentBranchLiveWithoutChangingOrganization()
    {
        var service = CreateService();
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
        var service = CreateService();
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
        var service = CreateService();
        await service.InitializeAsync();
        await service.SwitchBranchAsync("branch-2");

        var restarted = CreateService();
        await restarted.InitializeAsync();

        Assert.Equal("branch-2", restarted.CurrentBranch?.Id);
        Assert.Equal("org-1", restarted.CurrentOrganization?.Id);
    }

    [Fact]
    public async Task SwitchRoleAsync_UpdatesCurrentRoleLiveAndRaisesSessionChanged()
    {
        var service = CreateService();
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
        var service = CreateService();
        await service.InitializeAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SwitchBranchAsync("branch-does-not-exist"));
    }

    [Fact]
    public async Task SwitchBranchAsync_PushesToRecentBranchIdsNewestFirstWithoutDuplicates()
    {
        var service = CreateService();
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
        var service = CreateService();
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
        var service = CreateService();
        await service.InitializeAsync();
        var raised = false;
        service.SessionChanged += (_, _) => raised = true;

        await service.ToggleFavoriteBranchAsync("branch-2");
        Assert.Contains("branch-2", service.FavoriteBranchIds);
        Assert.True(raised);

        await service.ToggleFavoriteBranchAsync("branch-2");
        Assert.DoesNotContain("branch-2", service.FavoriteBranchIds);

        await service.ToggleFavoriteBranchAsync("branch-3");
        var restarted = CreateService();
        await restarted.InitializeAsync();
        Assert.Contains("branch-3", restarted.FavoriteBranchIds);
    }

    [Fact]
    public async Task InitializeAsync_DemoModeDisabled_NeverReachesTheSeededOrganizationEvenWithNoSettingsFile()
    {
        // The P0 regression test's mirror image: confirms Demo Context requires the flag, not just
        // "no real membership" - see the NoBusinessContext region below for the primary P0 test.
        _demoModeProvider.IsEnabled = false;
        var service = CreateService();

        await service.InitializeAsync();

        Assert.NotEqual(DesktopContextState.DemoContext, service.ContextState);
        Assert.Null(service.CurrentOrganization);
    }

    [Fact]
    public async Task InitializeAsync_DemoModeEnabled_SkipsRealResolutionEntirelyEvenWhenARealContextWouldResolve()
    {
        // Demo Context is checked before real resolution is even attempted (see InitializeAsync's own
        // doc comment) - a deliberate developer choice, decoupled from what account is actually signed in.
        _salonContextService.Context = new SalonContext("salon-1", "Glow Salon", IsOwner: true, MembershipRole: null);
        var service = CreateService();

        await service.InitializeAsync();

        Assert.Equal(DesktopContextState.DemoContext, service.ContextState);
        Assert.Equal("org-1", service.CurrentOrganization?.Id);
    }

    // ---- Reception Production Integration: real membership resolution ----

    [Fact]
    public async Task InitializeAsync_OwnerSalonContext_ResolvesToOrganizationOwnerAndSkipsTheFakeOrganizationPath()
    {
        _demoModeProvider.IsEnabled = false;
        _salonContextService.Context = new SalonContext("salon-1", "Glow Salon", IsOwner: true, MembershipRole: null);
        var service = CreateService();

        await service.InitializeAsync();

        Assert.Equal("salon-1", service.CurrentOrganization?.Id);
        Assert.Equal("Glow Salon", service.CurrentOrganization?.Name);
        Assert.Equal(WorkspaceRole.OrganizationOwner, service.CurrentRole);
        Assert.Null(service.CurrentBranch);
        Assert.Equal(DesktopContextState.OwnerContext, service.ContextState);
    }

    [Fact]
    public async Task InitializeAsync_AcceptedReceptionistInvite_ResolvesToReceptionRole()
    {
        _demoModeProvider.IsEnabled = false;
        _salonContextService.Context = new SalonContext("salon-9", "Glow Salon", IsOwner: false, MembershipRole: "RECEPTIONIST");
        var service = CreateService();

        await service.InitializeAsync();

        Assert.Equal(WorkspaceRole.Reception, service.CurrentRole);
        Assert.Equal("salon-9", service.CurrentOrganization?.Id);
        Assert.Equal(DesktopContextState.StaffContext, service.ContextState);
    }

    [Fact]
    public async Task InitializeAsync_AcceptedManagerInvite_ResolvesToOrganizationManagerRole()
    {
        _demoModeProvider.IsEnabled = false;
        _salonContextService.Context = new SalonContext("salon-9", "Glow Salon", IsOwner: false, MembershipRole: "MANAGER");
        var service = CreateService();

        await service.InitializeAsync();

        Assert.Equal(WorkspaceRole.OrganizationManager, service.CurrentRole);
        Assert.Equal(DesktopContextState.StaffContext, service.ContextState);
    }

    [Fact]
    public async Task SwitchRoleAsync_SessionIsOwnerContext_Throws()
    {
        _demoModeProvider.IsEnabled = false;
        _salonContextService.Context = new SalonContext("salon-9", "Glow Salon", IsOwner: false, MembershipRole: "RECEPTIONIST");
        var service = CreateService();
        await service.InitializeAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SwitchRoleAsync(WorkspaceRole.PlatformOwner));
        Assert.Equal(WorkspaceRole.Reception, service.CurrentRole);
    }

    [Fact]
    public async Task SwitchBranchAsync_SessionIsStaffContext_Throws()
    {
        _demoModeProvider.IsEnabled = false;
        _salonContextService.Context = new SalonContext("salon-9", "Glow Salon", IsOwner: false, MembershipRole: "RECEPTIONIST");
        var service = CreateService();
        await service.InitializeAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SwitchBranchAsync("branch-1"));
    }

    [Fact]
    public async Task SwitchRoleAsync_DemoContext_StillWorksUnchanged()
    {
        // Confirms the guard is scoped to real (Owner/Staff) sessions only - Demo Context's own
        // dev/demo experience (manual role switching for previewing the seeded UI) is untouched.
        var service = CreateService();
        await service.InitializeAsync();

        await service.SwitchRoleAsync(WorkspaceRole.Inventory);

        Assert.Equal(WorkspaceRole.Inventory, service.CurrentRole);
    }

    // ---- Phase 2B Context State Hardening: NoBusinessContext - the P0 fix ----

    [Fact]
    public async Task InitializeAsync_NoRealContextAndDemoModeDisabled_ResolvesToNoBusinessContextNotTheSeededOrganization()
    {
        // The primary regression test for this phase's own reason to exist: an account with no real
        // business context, and no explicit demo flag, must never be silently handed the fake org.
        _demoModeProvider.IsEnabled = false;
        var service = CreateService();

        await service.InitializeAsync();

        Assert.Equal(DesktopContextState.NoBusinessContext, service.ContextState);
        Assert.Null(service.CurrentOrganization);
        Assert.Null(service.CurrentBranch);
        Assert.Empty(service.AvailableBranches);
    }

    [Fact]
    public async Task InitializeAsync_NoRealContextAndDemoModeDisabled_NeverGrantsPlatformOwner()
    {
        // The exact privilege-escalation half of the P0 risk: no account should ever be silently
        // handed the highest-privileged role just because real resolution came back empty.
        _demoModeProvider.IsEnabled = false;
        var service = CreateService();

        await service.InitializeAsync();

        Assert.NotEqual(WorkspaceRole.PlatformOwner, service.CurrentRole);
        Assert.Equal(WorkspaceRole.Support, service.CurrentRole);
    }

    [Fact]
    public async Task SwitchRoleAsync_NoBusinessContext_StillWorksUnchanged()
    {
        // NoBusinessContext is not a real session either - switching should behave the same as it
        // always has for any non-real context (previously "HasRealMembership == false").
        _demoModeProvider.IsEnabled = false;
        var service = CreateService();
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

    private sealed class StubDemoModeProvider : IDemoModeProvider
    {
        public bool IsEnabled { get; set; }
    }
}
