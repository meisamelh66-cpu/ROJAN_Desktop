using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>Phase C-2: reusable, decoupled from any specific ViewModel via its own Dependency Properties - same shape as every other Dashboard card control this phase set added.</summary>
public partial class StaffStatusPanel : UserControl
{
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(nameof(Items), typeof(IEnumerable), typeof(StaffStatusPanel), new PropertyMetadata(null));

    public static readonly DependencyProperty ViewAllCommandProperty =
        DependencyProperty.Register(nameof(ViewAllCommand), typeof(ICommand), typeof(StaffStatusPanel), new PropertyMetadata(null));

    public StaffStatusPanel()
    {
        InitializeComponent();
    }

    public IEnumerable? Items
    {
        get => (IEnumerable?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public ICommand? ViewAllCommand
    {
        get => (ICommand?)GetValue(ViewAllCommandProperty);
        set => SetValue(ViewAllCommandProperty, value);
    }
}
