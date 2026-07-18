using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Navigation;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Shell.Navigation;

namespace Rojan.Desktop.Shell;

/// <summary>
/// Drives MainWindow's chrome: which sidebar section is selected, and
/// triggering navigation for the sections that have a real page. No
/// business logic - lives in Shell (not Presentation) because it is
/// specific to this window's frame, not a reusable page ViewModel, the
/// same reasoning that keeps the concrete NavigationService in Shell.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private NavigationItem _selectedNavigationItem = null!;

    public MainWindowViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        NavigationItems = new ObservableCollection<NavigationItem>
        {
            new(ShellSection.Dashboard, "Dashboard"),
            new(ShellSection.Crm, "CRM"),
            new(ShellSection.Booking, "Booking"),
            new(ShellSection.AiAssistant, "AI Assistant"),
            new(ShellSection.Reports, "Reports"),
            new(ShellSection.Settings, "Settings"),
        };

        SelectNavigationItemCommand = new RelayCommand(parameter =>
        {
            if (parameter is NavigationItem item)
            {
                SelectedNavigationItem = item;
            }
        });

        // Goes through the property setter (not a raw field assignment) so
        // the initial selection navigates too - Dashboard is both the
        // default item and the only section with a real page today.
        SelectedNavigationItem = NavigationItems[0];
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public NavigationItem SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (SetProperty(ref _selectedNavigationItem, value))
            {
                OnPropertyChanged(nameof(PlaceholderVisibility));

                // Only Dashboard has a page today - the other five sections
                // (CRM/Booking/AI Assistant/Reports/Settings) are sidebar
                // placeholders per Phase 04/06A scope, nothing to navigate
                // to yet.
                if (value.Section == ShellSection.Dashboard)
                {
                    _navigationService.NavigateTo<DashboardPageViewModel>();
                }
            }
        }
    }

    /// <summary>Visible only for the still-unimplemented sections, so the Phase 04 "coming soon" placeholder doesn't sit behind a real page's content.</summary>
    public Visibility PlaceholderVisibility =>
        SelectedNavigationItem.Section == ShellSection.Dashboard ? Visibility.Collapsed : Visibility.Visible;

    public ICommand SelectNavigationItemCommand { get; }
}
