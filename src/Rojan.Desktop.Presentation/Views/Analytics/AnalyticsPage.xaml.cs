using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Analytics;

/// <summary>Analytics Dashboard layout. No code-behind logic - DataContext is the resolved AnalyticsPageViewModel, set by WPF's implicit DataTemplate resolution.</summary>
public partial class AnalyticsPage : UserControl
{
    public AnalyticsPage()
    {
        InitializeComponent();
    }
}
