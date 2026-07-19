using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Presentation.Dialogs;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Navigation;

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
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase, IDialogService
{
    private readonly INavigationService _navigationService;
    private NavigationItem _selectedNavigationItem = null!;
    private bool _isSidebarExpanded = true;
    private bool _canGoBack;
    private bool _canGoForward;
    private bool _isNotificationPanelOpen;
    private string _statusMessage = Strings.Common_Ready;
    private object? _activeDialog;

    public MainWindowViewModel(IModuleRegistry moduleRegistry, INavigationService navigationService)
    {
        _navigationService = navigationService;

        NavigationItems = new ObservableCollection<NavigationItem>(
            moduleRegistry.Modules.Select(descriptor => new NavigationItem(descriptor)));

        Breadcrumbs = new ObservableCollection<string>();
        Notifications = new ObservableCollection<NotificationItem>();
        Notifications.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNotifications));

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

        // Goes through the property setter (not a raw field assignment) so
        // the initial selection navigates too - the first module in
        // display order is both the default item and the first thing
        // shown.
        SelectedNavigationItem = NavigationItems[0];
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public ObservableCollection<string> Breadcrumbs { get; }

    public ObservableCollection<NotificationItem> Notifications { get; }

    /// <summary>No producer exists yet, so this is always false today - wired now so the notification panel shows the right thing (list vs. "No notifications.") the moment one does.</summary>
    public bool HasNotifications => Notifications.Count > 0;

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
}
