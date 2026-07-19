using System.Windows;
using System.Windows.Shell;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Shell.Navigation;

namespace Rojan.Desktop.Shell;

/// <summary>
/// Shell window chrome - composition root wires this to
/// <see cref="NavigationService"/> and <see cref="MainWindowViewModel"/>.
/// The three window-command handlers below are the one exception to "no
/// code-behind": window chrome (minimize/maximize/close) is Shell's own
/// documented responsibility (docs/architecture/01-desktop-shell.md §2),
/// and WPF's SystemCommands need a live Window reference a ViewModel
/// should never hold - each handler is a single call, not logic.
///
/// Phase 19A: <see cref="FlowDirection"/> is set once here, from the
/// already-initialized <see cref="ILocalizationService.CurrentLanguage"/>
/// (App.OnStartup calls <c>InitializeAsync</c> before this constructor
/// ever runs) - WPF cascades FlowDirection to the entire visual tree by
/// default (Grid/StackPanel/DockPanel layout mirrors, and the Dialog
/// region's ContentControl is a descendant of this Window so dialogs
/// inherit it too), so this single assignment is genuinely all Windows/
/// Dialogs/Navigation/Forms/Cards/Menus need for RTL/LTR per Phase 19A's
/// requirement list - no per-View changes.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel, NavigationService navigationService, ILocalizationService localizationService, ICultureService cultureService)
    {
        InitializeComponent();
        DataContext = viewModel;
        navigationService.Attach(NavigationHost);
        FlowDirection = cultureService.GetFlowDirection(localizationService.CurrentLanguage.IsRightToLeft);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        SystemCommands.MinimizeWindow(this);
    }

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(this);
        }
        else
        {
            SystemCommands.MaximizeWindow(this);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        SystemCommands.CloseWindow(this);
    }
}
