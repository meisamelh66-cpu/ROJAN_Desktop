using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Infrastructure.Organizations;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Navigation;
using Rojan.Desktop.Presentation.ViewModels.Modules;
using Rojan.Desktop.Shell;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>
/// Exercises <see cref="MainWindowViewModel"/>'s permission-aware
/// Navigation Generation - Phase 22's requirement that the sidebar is
/// built dynamically from permissions, live-refreshing on a branch/role
/// switch (<see cref="Presentation.Organizations.ICurrentSessionService.SessionChanged"/>),
/// while every pre-Phase-22 module (no <see cref="ModuleMetadata.RequiredPermission"/>)
/// stays unconditionally visible - the additive/non-breaking guarantee.
/// </summary>
public sealed class MainWindowViewModelNavigationTests
{
    private static ModuleDescriptor Module(string id, Permission? requiredPermission = null) =>
        new(new ModuleMetadata(id, id, string.Empty, 0, requiredPermission), _ => new PlaceholderModuleViewModel(id));

    private static OrganizationQueryService CreateOrganizationQueryService() => new(new FakeOrganizationRepository());

    [Fact]
    public void NavigationItems_ModuleWithNoRequiredPermission_IsAlwaysVisible()
    {
        var modules = new[] { Module("dashboard") };
        var session = new StubCurrentSessionService { CurrentRole = WorkspaceRole.Support };
        var viewModel = new MainWindowViewModel(new StubModuleRegistry(modules), new StubNavigationService(), new PermissionEngine(), session, CreateOrganizationQueryService());

        Assert.Single(viewModel.NavigationItems);
        Assert.Equal("dashboard", viewModel.NavigationItems[0].Descriptor.Metadata.Id);
    }

    [Fact]
    public void NavigationItems_ModuleRequiringUngrantedPermission_IsHiddenForThatRole()
    {
        var modules = new[] { Module("dashboard"), Module("organizations", Permission.OrganizationManage) };
        var session = new StubCurrentSessionService { CurrentRole = WorkspaceRole.Support };
        var viewModel = new MainWindowViewModel(new StubModuleRegistry(modules), new StubNavigationService(), new PermissionEngine(), session, CreateOrganizationQueryService());

        Assert.DoesNotContain(viewModel.NavigationItems, item => item.Descriptor.Metadata.Id == "organizations");
    }

    [Fact]
    public void NavigationItems_ModuleRequiringGrantedPermission_IsVisibleForThatRole()
    {
        var modules = new[] { Module("dashboard"), Module("organizations", Permission.OrganizationManage) };
        var session = new StubCurrentSessionService { CurrentRole = WorkspaceRole.PlatformOwner };
        var viewModel = new MainWindowViewModel(new StubModuleRegistry(modules), new StubNavigationService(), new PermissionEngine(), session, CreateOrganizationQueryService());

        Assert.Contains(viewModel.NavigationItems, item => item.Descriptor.Metadata.Id == "organizations");
    }

    [Fact]
    public void NavigationItems_AfterRoleSwitchViaSessionChanged_RefreshesLiveWithoutRestart()
    {
        var modules = new[] { Module("dashboard"), Module("organizations", Permission.OrganizationManage) };
        var session = new StubCurrentSessionService { CurrentRole = WorkspaceRole.Support };
        var viewModel = new MainWindowViewModel(new StubModuleRegistry(modules), new StubNavigationService(), new PermissionEngine(), session, CreateOrganizationQueryService());
        Assert.DoesNotContain(viewModel.NavigationItems, item => item.Descriptor.Metadata.Id == "organizations");

        session.CurrentRole = WorkspaceRole.PlatformOwner;
        session.RaiseSessionChanged();

        Assert.Contains(viewModel.NavigationItems, item => item.Descriptor.Metadata.Id == "organizations");
    }

    [Fact]
    public void NavigationItems_AfterRoleSwitchHidesCurrentSelection_FallsBackToFirstVisibleItem()
    {
        var modules = new[] { Module("dashboard"), Module("organizations", Permission.OrganizationManage) };
        var session = new StubCurrentSessionService { CurrentRole = WorkspaceRole.PlatformOwner };
        var viewModel = new MainWindowViewModel(new StubModuleRegistry(modules), new StubNavigationService(), new PermissionEngine(), session, CreateOrganizationQueryService());
        viewModel.SelectedNavigationItem = viewModel.NavigationItems.Single(item => item.Descriptor.Metadata.Id == "organizations");

        session.CurrentRole = WorkspaceRole.Support;
        session.RaiseSessionChanged();

        Assert.Equal("dashboard", viewModel.SelectedNavigationItem.Descriptor.Metadata.Id);
    }
}
