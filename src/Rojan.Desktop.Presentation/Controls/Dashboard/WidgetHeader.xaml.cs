using System.Windows;
using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>Compact per-widget title. No logic beyond the one dependency property.</summary>
public partial class WidgetHeader : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(WidgetHeader),
            new PropertyMetadata(string.Empty));

    public WidgetHeader()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}
