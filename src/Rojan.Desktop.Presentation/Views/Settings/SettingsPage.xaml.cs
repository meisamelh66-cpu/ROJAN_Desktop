using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Settings;

/// <summary>Language settings layout. No code-behind logic - DataContext is the resolved SettingsPageViewModel, set by WPF's implicit DataTemplate resolution.</summary>
public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
    }
}
