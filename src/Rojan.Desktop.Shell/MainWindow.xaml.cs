using System.Windows;
using Rojan.Desktop.Shell.Navigation;

namespace Rojan.Desktop.Shell;

/// <summary>Shell window chrome - composition root wires this to <see cref="NavigationService"/>; owns no business logic.</summary>
public partial class MainWindow : Window
{
    public MainWindow(NavigationService navigationService)
    {
        InitializeComponent();
        navigationService.Attach(NavigationHost);
    }
}
