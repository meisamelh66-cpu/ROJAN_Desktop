using Rojan.Desktop.Application.Organizations;
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
/// branch's own organization).
/// </summary>
public sealed class CurrentSessionServiceTests : IDisposable
{
    private readonly string _settingsFilePath;
    private readonly OrganizationQueryService _queryService;

    public CurrentSessionServiceTests()
    {
        _settingsFilePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "session.json");
        _queryService = new OrganizationQueryService(new FakeOrganizationRepository());
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
        var service = new CurrentSessionService(_queryService, _settingsFilePath);

        await service.InitializeAsync();

        Assert.Equal("org-1", service.CurrentOrganization?.Id);
        Assert.Equal("branch-1", service.CurrentBranch?.Id);
        Assert.Equal(WorkspaceRole.PlatformOwner, service.CurrentRole);
    }

    [Fact]
    public async Task InitializeAsync_ScopesAvailableBranchesToTheCurrentOrganizationOnly()
    {
        var service = new CurrentSessionService(_queryService, _settingsFilePath);

        await service.InitializeAsync();

        Assert.Equal(2, service.AvailableBranches.Count);
        Assert.All(service.AvailableBranches, b => Assert.Equal("org-1", b.OrganizationId));
    }

    [Fact]
    public async Task SwitchBranchAsync_ToBranchInSameOrganization_UpdatesCurrentBranchLiveWithoutChangingOrganization()
    {
        var service = new CurrentSessionService(_queryService, _settingsFilePath);
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
        var service = new CurrentSessionService(_queryService, _settingsFilePath);
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
        var service = new CurrentSessionService(_queryService, _settingsFilePath);
        await service.InitializeAsync();
        await service.SwitchBranchAsync("branch-2");

        var restarted = new CurrentSessionService(_queryService, _settingsFilePath);
        await restarted.InitializeAsync();

        Assert.Equal("branch-2", restarted.CurrentBranch?.Id);
        Assert.Equal("org-1", restarted.CurrentOrganization?.Id);
    }

    [Fact]
    public async Task SwitchRoleAsync_UpdatesCurrentRoleLiveAndRaisesSessionChanged()
    {
        var service = new CurrentSessionService(_queryService, _settingsFilePath);
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
        var service = new CurrentSessionService(_queryService, _settingsFilePath);
        await service.InitializeAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SwitchBranchAsync("branch-does-not-exist"));
    }
}
