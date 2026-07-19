using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Services;

/// <summary>Service catalog/profile layout. No code-behind logic - DataContext is the resolved ServicePageViewModel, set by WPF's implicit DataTemplate resolution.</summary>
public partial class ServicePage : UserControl
{
    public ServicePage()
    {
        InitializeComponent();
    }
}
