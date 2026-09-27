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
///
/// Page Stability: a reload of content that is already on screen is a
/// refresh, not a fresh load - the owning ViewModel keeps <see cref="State"/>
/// at Loaded/Empty and raises <see cref="IsRefreshing"/> instead, so the
/// content (and any selection/inputs inside it) stays visible under a thin
/// progress indicator. A refresh that fails keeps the content too: the
/// ViewModel sets <see cref="ErrorMessage"/> without switching to Error, and
/// <see cref="HasInlineError"/> shows it as a compact row (with Retry) above
/// the still-visible content instead of replacing it.
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
            new PropertyMetadata(DashboardState.Loading, OnInlineErrorInputChanged));

    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.Register(
            nameof(ErrorMessage),
            typeof(string),
            typeof(DashboardWidget),
            new PropertyMetadata(null, OnInlineErrorInputChanged));

    public static readonly DependencyProperty IsRefreshingProperty =
        DependencyProperty.Register(
            nameof(IsRefreshing),
            typeof(bool),
            typeof(DashboardWidget),
            new PropertyMetadata(false));

    private static readonly DependencyPropertyKey HasInlineErrorPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HasInlineError),
            typeof(bool),
            typeof(DashboardWidget),
            new PropertyMetadata(false));

    public static readonly DependencyProperty HasInlineErrorProperty = HasInlineErrorPropertyKey.DependencyProperty;

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

    /// <summary>True while already-displayed content is being reloaded in place - see this class's own doc comment.</summary>
    public bool IsRefreshing
    {
        get => (bool)GetValue(IsRefreshingProperty);
        set => SetValue(IsRefreshingProperty, value);
    }

    /// <summary>True when <see cref="ErrorMessage"/> belongs to a failed refresh (content still shown), not to a failed initial load (State Error).</summary>
    public bool HasInlineError => (bool)GetValue(HasInlineErrorProperty);

    /// <summary>The rule behind <see cref="HasInlineError"/> - public and static so it is testable without constructing a WPF control.</summary>
    public static bool ComputeHasInlineError(DashboardState state, string? errorMessage) =>
        state.HasSettledResult() && !string.IsNullOrEmpty(errorMessage);

    private static void OnInlineErrorInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var widget = (DashboardWidget)d;
        widget.SetValue(HasInlineErrorPropertyKey, ComputeHasInlineError(widget.State, widget.ErrorMessage));
    }
}
