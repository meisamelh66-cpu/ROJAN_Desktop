using System.Windows;
using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>
/// Static content card - a titled GlassCard wrapper for content that has
/// no load state of its own (Quick Actions, the AI Assistant placeholder).
/// For data that comes from a repository and can be Loading/Empty/Error,
/// use <see cref="DashboardWidget"/> instead. Visuals live entirely in the
/// implicit style in Themes/DashboardComponents.xaml - this class is purely
/// the two dependency properties.
/// </summary>
public class DashboardCard : ContentControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(DashboardCard),
            new PropertyMetadata(null));

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}
