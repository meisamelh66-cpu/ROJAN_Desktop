using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>Phase C-1 (Analytics Row): reusable, decoupled from any specific ViewModel via its own Dependency Properties.</summary>
public partial class TopServicesCard : UserControl
{
    public static readonly DependencyProperty ChartProperty =
        DependencyProperty.Register(nameof(Chart), typeof(ChartDefinitionDto), typeof(TopServicesCard), new PropertyMetadata(null));

    public static readonly DependencyProperty ViewAllServicesCommandProperty =
        DependencyProperty.Register(nameof(ViewAllServicesCommand), typeof(ICommand), typeof(TopServicesCard), new PropertyMetadata(null));

    public TopServicesCard()
    {
        InitializeComponent();
    }

    public ChartDefinitionDto? Chart
    {
        get => (ChartDefinitionDto?)GetValue(ChartProperty);
        set => SetValue(ChartProperty, value);
    }

    public ICommand? ViewAllServicesCommand
    {
        get => (ICommand?)GetValue(ViewAllServicesCommandProperty);
        set => SetValue(ViewAllServicesCommandProperty, value);
    }
}
