using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Salons;

/// <summary>My Salon layout. No code-behind logic - DataContext is the resolved SalonPageViewModel, set by WPF's implicit DataTemplate resolution.</summary>
public partial class SalonPage : UserControl
{
    public SalonPage()
    {
        InitializeComponent();
    }
}
