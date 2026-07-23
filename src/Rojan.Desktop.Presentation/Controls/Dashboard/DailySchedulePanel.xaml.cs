using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>Phase C-2: reusable, decoupled from any specific ViewModel via its own Dependency Properties.</summary>
public partial class DailySchedulePanel : UserControl
{
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(nameof(Items), typeof(IEnumerable), typeof(DailySchedulePanel), new PropertyMetadata(null));

    public static readonly DependencyProperty ViewFullScheduleCommandProperty =
        DependencyProperty.Register(nameof(ViewFullScheduleCommand), typeof(ICommand), typeof(DailySchedulePanel), new PropertyMetadata(null));

    public DailySchedulePanel()
    {
        InitializeComponent();
    }

    public IEnumerable? Items
    {
        get => (IEnumerable?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public ICommand? ViewFullScheduleCommand
    {
        get => (ICommand?)GetValue(ViewFullScheduleCommandProperty);
        set => SetValue(ViewFullScheduleCommandProperty, value);
    }
}
