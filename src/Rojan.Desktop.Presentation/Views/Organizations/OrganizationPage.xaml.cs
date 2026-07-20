using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Organizations;

/// <summary>Organization &amp; Branches admin layout. No code-behind logic - DataContext is the resolved OrganizationPageViewModel, set by WPF's implicit DataTemplate resolution.</summary>
public partial class OrganizationPage : UserControl
{
    public OrganizationPage()
    {
        InitializeComponent();
    }
}
