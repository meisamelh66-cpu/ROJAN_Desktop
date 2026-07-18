using System.Windows;
using Rojan.Desktop.Shell.Navigation;

namespace Rojan.Desktop.Shell;

/// <summary>Shell window chrome - composition root wires this to <see cref="NavigationService"/> and <see cref="MainWindowViewModel"/>; owns no business logic.</summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel, NavigationService navigationService)
    {
        InitializeComponent();
        DataContext = viewModel;
        navigationService.Attach(NavigationHost);
    }
}
