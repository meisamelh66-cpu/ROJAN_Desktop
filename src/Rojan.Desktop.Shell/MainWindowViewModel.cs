using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Help;
using Rojan.Desktop.Application.Notifications;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Presentation.Dialogs;
using Rojan.Desktop.Presentation.Help;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Navigation;
using Rojan.Desktop.Presentation.Notifications;
using Rojan.Desktop.Presentation.Organizations;
using Rojan.Desktop.Presentation.ViewModels.Help;
using Rojan.Desktop.Presentation.ViewModels.Notifications;

namespace Rojan.Desktop.Shell;

/// <summary>
/// The shell orchestrator: drives every region MainWindow hosts
/// (navigation sidebar, header breadcrumb/status, notification panel,
/// dialog host). No business logic - lives in Shell (not Presentation)
/// because it is specific to this window's frame, not a reusable page
/// ViewModel, the same reasoning that keeps the concrete NavigationService
/// and ModuleRegistry in Shell. Implements <see cref="IDialogService"/>
/// directly (same alias-registration pattern App.xaml.cs already uses for
/// NavigationService/INavigationService) - Phase 15 is the first producer
/// of <see cref="ActiveDialog"/>, filling in the extension point that
/// property's doc comment has named since Phase 07.
///
/// Phase 22: <see cref="NavigationItems"/> is now permission-filtered -
/// built from every registered module whose <c>RequiredPermission</c> is
/// either unset (every pre-Phase-22 module) or granted to
/// <see cref="ICurrentSessionService.CurrentRole"/>. Rebuilt live whenever
/// <see cref="ICurrentSessionService.SessionChanged"/> fires (a branch or
/// role switch), so the sidebar always reflects the active session.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase, IDialogService
{
    private readonly INavigationService _navigationService;
    private readonly IPermissionEngine _permissionEngine;
    private readonly ICurrentSessionService _currentSessionService;
    private readonly IOrganizationQueryService _organizationQueryService;
    private readonly IHelpQueryService _helpQueryService;
    private readonly IHelpContentResolver _helpContentResolver;
    private readonly IHelpSearchService _helpSearchService;
    private readonly IHelpFavoritesStore _helpFavoritesStore;
    private readonly IHelpRecentlyViewedStore _helpRecentlyViewedStore;
    private readonly IReadOnlyList<ModuleDescriptor> _allModules;
    private NavigationItem _selectedNavigationItem = null!;
    private bool _isSidebarExpanded = true;
    private bool _canGoBack;
    private bool _canGoForward;
    private bool _isNotificationPanelOpen;
    private string _statusMessage = Strings.Common_Ready;
    private object? _activeDialog;

    private List<BranchSwitcherGroup> _allBranchGroups = [];
    private Dictionary<string, BranchDto> _allBranchesById = [];
    private string _branchSearchText = string.Empty;
    private bool _isBranchSwitcherOpen;

    public MainWindowViewModel(
        IModuleRegistry moduleRegistry,
        INavigationService navigationService,
        IPermissionEngine permissionEngine,
        ICurrentSessionService currentSessionService,
        IOrganizationQueryService organizationQueryService,
        IHelpQueryService helpQueryService,
        IHelpContentResolver helpContentResolver,
        IHelpSearchService helpSearchService,
        IHelpFavoritesStore helpFavoritesStore,
        IHelpRecentlyViewedStore helpRecentlyViewedStore,
        INotificationService notificationService,
        INotificationContentResolver notificationContentResolver,
        INotificationSearchService notificationSearchService,
        IToastDismissScheduler toastDismissScheduler)
    {
        _navigationService = navigationService;
        _permissionEngine = permissionEngine;
        _currentSessionService = currentSessionService;
        _organizationQueryService = organizationQueryService;
        _helpQueryService = helpQueryService;
        _helpContentResolver = helpContentResolver;
        _helpSearchService = helpSearchService;
        _helpFavoritesStore = helpFavoritesStore;
        _helpRecentlyViewedStore = helpRecentlyViewedStore;
        _allModules = moduleRegistry.Modules;

        NavigationItems = new ObservableCollection<NavigationItem>(BuildVisibleNavigationItems());

        Breadcrumbs = new ObservableCollection<string>();

        NotificationCenter = new NotificationCenterViewModel(notificationService, notificationContentResolver, notificationSearchService);
        ToastHost = new ToastHostViewModel(notificationService, notificationContentResolver, toastDismissScheduler);
        _ = NotificationCenter.InitializeAsync();

        BranchGroups = new ObservableCollection<BranchSwitcherGroup>();
        RecentBranches = new ObservableCollection<BranchDto>();
        FavoriteBranches = new ObservableCollection<BranchDto>();

        SelectNavigationItemCommand = new RelayCommand(parameter =>
        {
            if (parameter is NavigationItem item)
            {
                SelectedNavigationItem = item;
            }
        });
        ToggleSidebarCommand = new RelayCommand(_ => IsSidebarExpanded = !IsSidebarExpanded);
        ToggleNotificationPanelCommand = new RelayCommand(_ => IsNotificationPanelOpen = !IsNotificationPanelOpen);
        GoBackCommand = new RelayCommand(_ => Navigate(_navigationService.GoBack), _ => CanGoBack);
        GoForwardCommand = new RelayCommand(_ => Navigate(_navigationService.GoForward), _ => CanGoForward);
        ToggleBranchSwitcherCommand = new RelayCommand(_ => IsBranchSwitcherOpen = !IsBranchSwitcherOpen);
        SelectBranchFromSwitcherCommand = new AsyncRelayCommand(parameter => SelectBranchFromSwitcherAsync((BranchDto)parameter!));
        ToggleFavoriteBranchCommand = new AsyncRelayCommand(parameter => ToggleFavoriteBranchAsync((BranchDto)parameter!));
        OpenHelpCommand = new AsyncRelayCommand(parameter => OpenHelpAsync(parameter as string));

        _currentSessionService.SessionChanged += (_, _) => OnSessionChanged();

        // Goes through the property setter (not a raw field assignment) so
        // the initial selection navigates too - the first module in
        // display order is both the default item and the first thing
        // shown.
        SelectedNavigationItem = NavigationItems[0];

        _ = LoadBranchSwitcherDataAsync();
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public ObservableCollection<string> Breadcrumbs { get; }

    /// <summary>Phase 27: Enterprise Notification Center - owns the panel's grouped/filtered/searched list, badge count, and Silent Mode toggle. Constructed once here (not per-open), since the Notification Center has no per-context state to seed.</summary>
    public NotificationCenterViewModel NotificationCenter { get; }

    /// <summary>Phase 27: the transient toast-popup overlay, entirely separate from <see cref="ActiveDialog"/> - see <see cref="ToastHostViewModel"/>'s own doc comment for why.</summary>
    public ToastHostViewModel ToastHost { get; }

    /// <summary>Header display form of <see cref="Breadcrumbs"/> - a single "Home › Dashboard" string, recomputed whenever the collection changes.</summary>
    public string BreadcrumbText => string.Join(" › ", Breadcrumbs);

    public NavigationItem SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (SetProperty(ref _selectedNavigationItem, value))
            {
                Navigate(() => _navigationService.NavigateTo(value.Descriptor));
                StatusMessage = Strings.Common_ViewingFormat.Replace("{0}", value.Title, StringComparison.Ordinal);
                Breadcrumbs.Clear();
                Breadcrumbs.Add(Strings.Common_Home);
                Breadcrumbs.Add(value.Title);
                OnPropertyChanged(nameof(BreadcrumbText));
            }
        }
    }

    public bool IsSidebarExpanded
    {
        get => _isSidebarExpanded;
        set => SetProperty(ref _isSidebarExpanded, value);
    }

    /// <summary>The Branch Switcher's data source - every branch in the current organization.</summary>
    public IReadOnlyList<BranchDto> AvailableBranches => _currentSessionService.AvailableBranches;

    /// <summary>The Branch Switcher's selection - setting this live-switches the active branch (see <see cref="ICurrentSessionService.SwitchBranchAsync"/>), no restart required.</summary>
    public BranchDto? CurrentBranch
    {
        get => _currentSessionService.CurrentBranch;
        set
        {
            if (value is not null && value.Id != _currentSessionService.CurrentBranch?.Id)
            {
                _ = SwitchBranchAsync(value.Id);
            }
        }
    }

    public string CurrentOrganizationName => _currentSessionService.CurrentOrganization?.Name ?? string.Empty;

    /// <summary>Every organization's branches, grouped - the flyout's "Organization grouping" section, filtered live by <see cref="BranchSearchText"/>.</summary>
    public ObservableCollection<BranchSwitcherGroup> BranchGroups { get; }

    /// <summary>The flyout's "Recently used branches" section, resolved from <see cref="ICurrentSessionService.RecentBranchIds"/>.</summary>
    public ObservableCollection<BranchDto> RecentBranches { get; }

    /// <summary>The flyout's "Favorite branches" section, resolved from <see cref="ICurrentSessionService.FavoriteBranchIds"/>.</summary>
    public ObservableCollection<BranchDto> FavoriteBranches { get; }

    public string BranchSearchText
    {
        get => _branchSearchText;
        set
        {
            if (SetProperty(ref _branchSearchText, value))
            {
                ApplyBranchSearchFilter();
            }
        }
    }

    public bool IsBranchSwitcherOpen
    {
        get => _isBranchSwitcherOpen;
        set => SetProperty(ref _isBranchSwitcherOpen, value);
    }

    public ICommand ToggleBranchSwitcherCommand { get; }

    public ICommand SelectBranchFromSwitcherCommand { get; }

    public ICommand ToggleFavoriteBranchCommand { get; }

    public bool CanGoBack
    {
        get => _canGoBack;
        private set => SetProperty(ref _canGoBack, value);
    }

    public bool CanGoForward
    {
        get => _canGoForward;
        private set => SetProperty(ref _canGoForward, value);
    }

    public bool IsNotificationPanelOpen
    {
        get => _isNotificationPanelOpen;
        set => SetProperty(ref _isNotificationPanelOpen, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Dialog region host - null means nothing is shown. Set via <see cref="ShowDialog"/>/<see cref="CloseDialog"/>, its <see cref="IDialogService"/> implementation.</summary>
    public object? ActiveDialog
    {
        get => _activeDialog;
        set => SetProperty(ref _activeDialog, value);
    }

    public void ShowDialog(object viewModel) => ActiveDialog = viewModel;

    public void CloseDialog() => ActiveDialog = null;

    /// <summary>Phase 26.3: the Smart Help Button's command - opens the Context Help Dialog for <c>moduleId</c> (falls back to the currently-selected module when no explicit id is passed, e.g. a header-level Help button with no per-control context).</summary>
    public ICommand OpenHelpCommand { get; }

    public ICommand SelectNavigationItemCommand { get; }

    public ICommand ToggleSidebarCommand { get; }

    public ICommand ToggleNotificationPanelCommand { get; }

    public ICommand GoBackCommand { get; }

    public ICommand GoForwardCommand { get; }

    /// <summary>Runs a navigation-service action, then republishes CanGoBack/CanGoForward - INavigationService exposes no change notification of its own, so this is the one place that stays in sync with it.</summary>
    private void Navigate(Action navigate)
    {
        navigate();
        CanGoBack = _navigationService.CanGoBack;
        CanGoForward = _navigationService.CanGoForward;
        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>Every module whose <c>RequiredPermission</c> is unset (every pre-Phase-22 module, unconditionally visible) or granted to the current session's role.</summary>
    private IEnumerable<NavigationItem> BuildVisibleNavigationItems() =>
        _allModules
            .Where(descriptor => descriptor.Metadata.RequiredPermission is not Permission requiredPermission
                || _permissionEngine.HasPermission(_currentSessionService.CurrentRole, requiredPermission))
            .Select(descriptor => new NavigationItem(descriptor));

    /// <summary>Rebuilds <see cref="NavigationItems"/> after a branch/role switch - re-selects the current item if it's still visible, otherwise falls back to the first visible one (never leaves <see cref="SelectedNavigationItem"/> pointing at a now-hidden module).</summary>
    private void RefreshNavigationItems()
    {
        var previousSelectionId = _selectedNavigationItem?.Descriptor.Metadata.Id;

        NavigationItems.Clear();
        foreach (var item in BuildVisibleNavigationItems())
        {
            NavigationItems.Add(item);
        }

        var stillVisible = NavigationItems.FirstOrDefault(item => item.Descriptor.Metadata.Id == previousSelectionId);
        SelectedNavigationItem = stillVisible ?? NavigationItems[0];
    }

    /// <summary>Republishes every header/sidebar property <see cref="ICurrentSessionService"/> backs, then rebuilds navigation - the one handler for both <see cref="SwitchBranchAsync"/> and a role switch fired from elsewhere (e.g. the Organization page's own Session section).</summary>
    private void OnSessionChanged()
    {
        OnPropertyChanged(nameof(CurrentBranch));
        OnPropertyChanged(nameof(AvailableBranches));
        OnPropertyChanged(nameof(CurrentOrganizationName));
        RefreshNavigationItems();
        RefreshRecentAndFavoriteBranches();
    }

    private async Task SwitchBranchAsync(string branchId)
    {
        await _currentSessionService.SwitchBranchAsync(branchId).ConfigureAwait(true);
    }

    /// <summary>
    /// Loads every organization and its branches - the Branch Switcher's
    /// full data set (unlike <see cref="AvailableBranches"/>, which is
    /// scoped to the current organization only) - so the flyout can group
    /// by organization and search across all of them, per this phase's
    /// "Organization grouping"/"Search" Branch Switcher requirements.
    /// </summary>
    private async Task LoadBranchSwitcherDataAsync()
    {
        var organizations = await _organizationQueryService.GetOrganizationsAsync().ConfigureAwait(true);
        var groups = new List<BranchSwitcherGroup>();
        var byId = new Dictionary<string, BranchDto>();

        foreach (var organization in organizations)
        {
            var branches = await _organizationQueryService.GetBranchesAsync(organization.Id).ConfigureAwait(true);
            groups.Add(new BranchSwitcherGroup(organization.Id, organization.Name, branches));
            foreach (var branch in branches)
            {
                byId[branch.Id] = branch;
            }
        }

        _allBranchGroups = groups;
        _allBranchesById = byId;
        ApplyBranchSearchFilter();
        RefreshRecentAndFavoriteBranches();
    }

    /// <summary>Rebuilds <see cref="BranchGroups"/> from <see cref="_allBranchGroups"/>, filtered by <see cref="BranchSearchText"/> against branch name/code - empty groups drop out entirely rather than showing as an empty header.</summary>
    private void ApplyBranchSearchFilter()
    {
        BranchGroups.Clear();

        var search = _branchSearchText.Trim();
        foreach (var group in _allBranchGroups)
        {
            var matching = search.Length == 0
                ? group.Branches
                : group.Branches.Where(b => b.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || b.Code.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

            if (matching.Count > 0)
            {
                BranchGroups.Add(new BranchSwitcherGroup(group.OrganizationId, group.OrganizationName, matching));
            }
        }
    }

    private void RefreshRecentAndFavoriteBranches()
    {
        RecentBranches.Clear();
        foreach (var branchId in _currentSessionService.RecentBranchIds)
        {
            if (_allBranchesById.TryGetValue(branchId, out var branch))
            {
                RecentBranches.Add(branch);
            }
        }

        FavoriteBranches.Clear();
        foreach (var branchId in _currentSessionService.FavoriteBranchIds)
        {
            if (_allBranchesById.TryGetValue(branchId, out var branch))
            {
                FavoriteBranches.Add(branch);
            }
        }
    }

    private async Task SelectBranchFromSwitcherAsync(BranchDto branch)
    {
        await SwitchBranchAsync(branch.Id).ConfigureAwait(true);
        IsBranchSwitcherOpen = false;
    }

    private async Task ToggleFavoriteBranchAsync(BranchDto branch)
    {
        await _currentSessionService.ToggleFavoriteBranchAsync(branch.Id).ConfigureAwait(true);
    }

    /// <summary>
    /// Constructs a fresh <see cref="HelpDialogViewModel"/> with this ViewModel's
    /// already-injected Help dependencies and shows it - the same
    /// constructed-via-<c>new</c>-by-its-opener shape as
    /// <c>PosCheckoutViewModel</c>/<c>ExportDialogViewModel</c>, since the
    /// dialog needs a runtime context (which module) no constructor-injected
    /// dependency can supply.
    /// </summary>
    private async Task OpenHelpAsync(string? moduleId)
    {
        var effectiveModuleId = string.IsNullOrEmpty(moduleId)
            ? SelectedNavigationItem.Descriptor.Metadata.Id
            : moduleId;

        var helpDialog = new HelpDialogViewModel(
            _helpQueryService,
            _helpContentResolver,
            _helpSearchService,
            _helpFavoritesStore,
            _helpRecentlyViewedStore,
            this);

        ShowDialog(helpDialog);
        await helpDialog.InitializeAsync(effectiveModuleId).ConfigureAwait(true);
    }
}
