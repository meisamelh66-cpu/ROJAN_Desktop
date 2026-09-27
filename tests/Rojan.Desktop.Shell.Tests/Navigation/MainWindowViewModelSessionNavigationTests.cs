using System.Collections.Specialized;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Infrastructure.Organizations;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Navigation;
using Rojan.Desktop.Presentation.ViewModels.Modules;
using Rojan.Desktop.Shell;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>
/// Shell Navigation (page stability): a session change must not clear and rebuild the sidebar, nor
/// recreate the page the user is on, unless that page is genuinely stale (its organization/branch
/// changed) or its module is no longer visible. Direct navigations the shell did not start itself
/// (Dashboard shortcut, Command Palette, Back/Forward) must move the sidebar selection with them.
/// </summary>
public sealed class MainWindowViewModelSessionNavigationTests
{
    private static ModuleDescriptor Module(string id, Permission? requiredPermission = null, Type? viewModelType = null) =>
        new(new ModuleMetadata(id, id, string.Empty, 0, requiredPermission), _ => new PlaceholderModuleViewModel(id), viewModelType);

    private static OrganizationDto Organization(string id) =>
        new(id, id, id, string.Empty, string.Empty, string.Empty, SubscriptionPlan.Professional, OrganizationStatus.Active,
            DateTimeOffset.UnixEpoch, id, string.Empty, string.Empty, string.Empty, "UTC", "fa-IR", "IRR");

    private static BranchDto Branch(string id, string organizationId) =>
        new(id, organizationId, id, id, string.Empty, string.Empty, string.Empty, string.Empty, "UTC", "IRR", BranchStatus.Active);

    private static MainWindowViewModel CreateViewModel(IReadOnlyList<ModuleDescriptor> modules, StubCurrentSessionService session, StubNavigationService navigation) =>
        new(
            new StubModuleRegistry(modules),
            navigation,
            new PermissionEngine(),
            session,
            new OrganizationQueryService(new FakeOrganizationRepository()),
            TestThemeServices.Service,
            TestLocalizationServices.Service,
            TestHelpServices.QueryService,
            TestHelpServices.ContentResolver,
            TestHelpServices.SearchService,
            TestHelpServices.CreateFavoritesStore(),
            TestHelpServices.CreateRecentlyViewedStore(),
            TestNotificationServices.CreateNotificationService(),
            TestNotificationServices.ContentResolver,
            TestNotificationServices.SearchService,
            TestNotificationServices.ToastDismissScheduler,
            TestSearchServices.IndexService,
            TestSearchServices.RankingService,
            TestSearchServices.CreateHistoryStore(),
            TestSearchServices.CreateFavoritesStore(),
            TestWorkspaceServices.CreateService(),
            TestWorkspaceServices.FloatingWindowManager,
            TestWorkspaceServices.ServiceProvider);

    private static StubCurrentSessionService OwnerSession() =>
        new() { ContextState = DesktopContextState.OwnerContext, CurrentRole = WorkspaceRole.PlatformOwner };

    [Fact]
    public void SelectedNavigationItem_SetToNull_IsIgnored_KeepsSelectionAndDoesNotNavigate()
    {
        // WPF's sidebar ListBox writes null back through its two-way SelectedItem binding whenever the
        // selected entry leaves NavigationItems - that must not clear the selection or, on the next
        // re-selection of the same module, recreate the current page.
        var navigation = new StubNavigationService();
        var sut = CreateViewModel([Module("dashboard"), Module("customers")], OwnerSession(), navigation);
        var navigationsBefore = navigation.NavigatedModuleIds.Count;

        sut.SelectedNavigationItem = null!;

        Assert.Equal("dashboard", sut.SelectedNavigationItem.Descriptor.Metadata.Id);

        sut.SelectedNavigationItem = sut.NavigationItems[0];

        Assert.Equal(navigationsBefore, navigation.NavigatedModuleIds.Count);
    }

    [Fact]
    public void SessionChanged_NothingVisibleChanged_DoesNotTouchTheSidebarOrRecreateThePage()
    {
        // e.g. a favorite-branch toggle - SessionChanged fires, but nothing the sidebar or the page
        // depends on actually changed.
        var navigation = new StubNavigationService();
        var session = OwnerSession();
        var sut = CreateViewModel([Module("dashboard"), Module("customers")], session, navigation);
        sut.SelectedNavigationItem = sut.NavigationItems[1];
        var itemsBefore = sut.NavigationItems.ToList();
        var navigationsBefore = navigation.NavigatedModuleIds.Count;
        var collectionChanges = 0;
        sut.NavigationItems.CollectionChanged += (_, _) => collectionChanges++;

        session.RaiseSessionChanged();

        Assert.Equal(0, collectionChanges);
        Assert.Equal(itemsBefore, sut.NavigationItems);
        Assert.Equal("customers", sut.SelectedNavigationItem.Descriptor.Metadata.Id);
        Assert.Equal(navigationsBefore, navigation.NavigatedModuleIds.Count);
        Assert.Empty(navigation.ReloadedModuleIds);
    }

    [Fact]
    public void SessionChanged_RoleRevealsAModule_InsertsItInPlace_KeepsCurrentEntriesAndPage()
    {
        var navigation = new StubNavigationService();
        var session = new StubCurrentSessionService { ContextState = DesktopContextState.DemoContext, CurrentRole = WorkspaceRole.Support };
        var sut = CreateViewModel([Module("accept-invite"), Module("organizations", Permission.OrganizationManage), Module("customers")], session, navigation);
        sut.SelectedNavigationItem = sut.NavigationItems.Single(item => item.Descriptor.Metadata.Id == "customers");
        var customersItem = sut.SelectedNavigationItem;
        var navigationsBefore = navigation.NavigatedModuleIds.Count;
        var actions = new List<NotifyCollectionChangedAction>();
        sut.NavigationItems.CollectionChanged += (_, e) => actions.Add(e.Action);

        session.CurrentRole = WorkspaceRole.PlatformOwner;
        session.RaiseSessionChanged();

        Assert.Equal(["accept-invite", "organizations", "customers"], sut.NavigationItems.Select(item => item.Descriptor.Metadata.Id));
        Assert.Equal([NotifyCollectionChangedAction.Add], actions); // no Reset - never cleared and rebuilt
        Assert.Same(customersItem, sut.SelectedNavigationItem);
        Assert.Equal(navigationsBefore, navigation.NavigatedModuleIds.Count);
        Assert.Empty(navigation.ReloadedModuleIds);
    }

    [Fact]
    public void SessionChanged_RoleHidesTheCurrentModule_FallsBackToFirstVisibleAndNavigatesThere()
    {
        var navigation = new StubNavigationService();
        var session = new StubCurrentSessionService { ContextState = DesktopContextState.DemoContext, CurrentRole = WorkspaceRole.PlatformOwner };
        var sut = CreateViewModel([Module("dashboard"), Module("organizations", Permission.OrganizationManage)], session, navigation);
        sut.SelectedNavigationItem = sut.NavigationItems.Single(item => item.Descriptor.Metadata.Id == "organizations");

        session.CurrentRole = WorkspaceRole.Support;
        session.RaiseSessionChanged();

        Assert.Equal("dashboard", sut.SelectedNavigationItem.Descriptor.Metadata.Id);
        Assert.Equal("dashboard", navigation.NavigatedModuleIds[^1]);
        Assert.DoesNotContain(sut.NavigationItems, item => item.Descriptor.Metadata.Id == "organizations");
    }

    [Fact]
    public void SessionChanged_BranchSwitched_ReloadsTheCurrentModuleOnceForTheNewContext()
    {
        var navigation = new StubNavigationService();
        var session = new StubCurrentSessionService
        {
            ContextState = DesktopContextState.DemoContext,
            CurrentOrganization = Organization("org-1"),
            CurrentBranch = Branch("branch-1", "org-1"),
        };
        var sut = CreateViewModel([Module("dashboard"), Module("customers")], session, navigation);
        sut.SelectedNavigationItem = sut.NavigationItems[1];
        var navigationsBefore = navigation.NavigatedModuleIds.Count;

        session.CurrentBranch = Branch("branch-2", "org-1");
        session.RaiseSessionChanged();

        Assert.Equal(["customers"], navigation.ReloadedModuleIds);
        Assert.Equal(navigationsBefore, navigation.NavigatedModuleIds.Count);
        Assert.Equal("customers", sut.SelectedNavigationItem.Descriptor.Metadata.Id);
    }

    [Fact]
    public void SessionChanged_FirstBusinessContext_DoesNotReloadThePageThatJustProducedIt()
    {
        // Accept Invite: a session with no organization yet gains its first one. The page showed no
        // business data to go stale, and reloading it would discard its own success state.
        var navigation = new StubNavigationService();
        var session = new StubCurrentSessionService { ContextState = DesktopContextState.NoBusinessContext };
        var sut = CreateViewModel([Module("dashboard"), Module("accept-invite")], session, navigation);
        Assert.Equal("accept-invite", sut.SelectedNavigationItem.Descriptor.Metadata.Id);

        session.ContextState = DesktopContextState.StaffContext;
        session.CurrentOrganization = Organization("salon-1");
        session.RaiseSessionChanged();

        Assert.Empty(navigation.ReloadedModuleIds);
        Assert.Equal("accept-invite", sut.SelectedNavigationItem.Descriptor.Metadata.Id);
    }

    [Fact]
    public void Navigated_TypedNavigationFromOutsideTheSidebar_SyncsSelectionWithoutNavigatingAgain()
    {
        // e.g. a Dashboard shortcut calling NavigateTo<CustomersViewModel>() directly - the sidebar used
        // to keep highlighting Dashboard, and clicking Dashboard then did nothing.
        var navigation = new StubNavigationService();
        var sut = CreateViewModel([Module("dashboard"), Module("customers", viewModelType: typeof(CustomersViewModel))], OwnerSession(), navigation);
        var navigationsBefore = navigation.NavigatedModuleIds.Count;

        navigation.RaiseNavigated(new CustomersViewModel(), moduleId: null);

        Assert.Equal("customers", sut.SelectedNavigationItem.Descriptor.Metadata.Id);
        Assert.Equal(navigationsBefore, navigation.NavigatedModuleIds.Count);
        Assert.Contains("customers", sut.BreadcrumbText, StringComparison.Ordinal);
    }

    [Fact]
    public void Navigated_DescriptorNavigationFromCommandPalette_SyncsSelectionByModuleId()
    {
        var navigation = new StubNavigationService();
        var sut = CreateViewModel([Module("dashboard"), Module("reports")], OwnerSession(), navigation);

        navigation.RaiseNavigated(new PlaceholderModuleViewModel("reports"), moduleId: "reports");

        Assert.Equal("reports", sut.SelectedNavigationItem.Descriptor.Metadata.Id);
    }

    [Fact]
    public void Navigated_ToSomethingNotInTheSidebar_LeavesTheSelectionUnchanged()
    {
        var navigation = new StubNavigationService();
        var sut = CreateViewModel([Module("dashboard"), Module("customers")], OwnerSession(), navigation);

        navigation.RaiseNavigated(new CustomersViewModel(), moduleId: null);

        Assert.Equal("dashboard", sut.SelectedNavigationItem.Descriptor.Metadata.Id);
    }

    private sealed class CustomersViewModel : ViewModelBase
    {
    }
}
