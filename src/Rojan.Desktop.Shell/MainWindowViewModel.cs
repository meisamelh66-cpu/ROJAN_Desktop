using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Shell.Navigation;

namespace Rojan.Desktop.Shell;

/// <summary>
/// Drives MainWindow's chrome: which sidebar section is selected. No
/// business logic - lives in Shell (not Presentation) because it is
/// specific to this window's frame, not a reusable page ViewModel, the
/// same reasoning that keeps the concrete NavigationService in Shell.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    private NavigationItem _selectedNavigationItem;

    public MainWindowViewModel()
    {
        NavigationItems = new ObservableCollection<NavigationItem>
        {
            new(ShellSection.Dashboard, "Dashboard"),
            new(ShellSection.Crm, "CRM"),
            new(ShellSection.Booking, "Booking"),
            new(ShellSection.AiAssistant, "AI Assistant"),
            new(ShellSection.Reports, "Reports"),
            new(ShellSection.Settings, "Settings"),
        };

        _selectedNavigationItem = NavigationItems[0];

        SelectNavigationItemCommand = new RelayCommand(parameter =>
        {
            if (parameter is NavigationItem item)
            {
                SelectedNavigationItem = item;
            }
        });
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public NavigationItem SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set => SetProperty(ref _selectedNavigationItem, value);
    }

    public ICommand SelectNavigationItemCommand { get; }
}
