using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>
/// State-aware GlassCard wrapper for repository-backed dashboard content -
/// shows a loading indicator, an empty-state message, or an error message
/// (with a Retry action) instead of <see cref="ContentControl.Content"/>
/// depending on <see cref="State"/>. Visuals live entirely in the implicit
/// style in Themes/DashboardComponents.xaml - this class is purely the
/// dependency properties.
/// </summary>
public class DashboardWidget : ContentControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(DashboardWidget),
            new PropertyMetadata(null));

    public static readonly DependencyProperty StateProperty =
        DependencyProperty.Register(
            nameof(State),
            typeof(DashboardState),
            typeof(DashboardWidget),
            new PropertyMetadata(DashboardState.Loading));

    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.Register(
            nameof(ErrorMessage),
            typeof(string),
            typeof(DashboardWidget),
            new PropertyMetadata(null));

    public static readonly DependencyProperty RetryCommandProperty =
        DependencyProperty.Register(
            nameof(RetryCommand),
            typeof(ICommand),
            typeof(DashboardWidget),
            new PropertyMetadata(null));

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public DashboardState State
    {
        get => (DashboardState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public string? ErrorMessage
    {
        get => (string?)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public ICommand? RetryCommand
    {
        get => (ICommand?)GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
    }
}
