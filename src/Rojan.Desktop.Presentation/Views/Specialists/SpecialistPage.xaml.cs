using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Specialists;

/// <summary>Specialist directory/profile layout. No code-behind logic - DataContext is the resolved SpecialistPageViewModel, set by WPF's implicit DataTemplate resolution.</summary>
public partial class SpecialistPage : UserControl
{
    public SpecialistPage()
    {
        InitializeComponent();
    }
}
