using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.AI;

/// <summary>AI Center page layout - no code-behind logic; every section is a plain XAML DataTrigger switch over <c>AiCenterPageViewModel.SelectedSection</c>, same shape as <c>Reporting.ReportingPage</c>.</summary>
public partial class AiCenterPage : UserControl
{
    public AiCenterPage()
    {
        InitializeComponent();
    }
}
