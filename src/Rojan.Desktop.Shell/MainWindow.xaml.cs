using System.Windows;
using System.Windows.Shell;
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
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel, NavigationService navigationService)
    {
        InitializeComponent();
        DataContext = viewModel;
        navigationService.Attach(NavigationHost);
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
