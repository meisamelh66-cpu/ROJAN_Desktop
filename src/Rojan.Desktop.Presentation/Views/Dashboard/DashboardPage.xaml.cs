using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Dashboard;

/// <summary>Dashboard layout. No code-behind logic - DataContext is the resolved DashboardPageViewModel, set by WPF's implicit DataTemplate resolution.</summary>
public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
    }
}
