using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>Phase C-2: reusable, decoupled from any specific ViewModel via its own Dependency Property.</summary>
public partial class RecentAlertsPanel : UserControl
{
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(nameof(Items), typeof(IEnumerable), typeof(RecentAlertsPanel), new PropertyMetadata(null));

    public RecentAlertsPanel()
    {
        InitializeComponent();
    }

    public IEnumerable? Items
    {
        get => (IEnumerable?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }
}
