using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using Rojan.Desktop.Presentation.Controls.Dashboard;

namespace Rojan.Desktop.Presentation.Controls.Shared;

/// <summary>
/// Label + value pair for a single KPI reading, plus (Dashboard Modernization
/// Sprint) an optional icon badge and a count-up reveal animation for Value,
/// and (Phase 35, Secure Amount Display) an optional mask-until-clicked
/// privacy state for monetary values. The count-up is a pure visual replay
/// of the real, already-bound Value string - it parses the leading numeric
/// run (keeping any prefix/suffix, e.g. a currency word, via the shared
/// KpiNumberParsing helper - also used by KpiChart's mini charts), animates
/// from 0 up to that real number, and re-renders the exact original format
/// on every tick. If Value isn't a plain formatted number the pattern can
/// safely parse, it's shown immediately as-is, unanimated - never a
/// fabricated intermediate shape.
/// </summary>
public partial class KPIValue : UserControl
{
    private const string MaskGlyph = "••••••••";
    private static readonly TimeSpan AutoHideDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RevealDuration = TimeSpan.FromMilliseconds(220);

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(KPIValue),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(KPIValue),
            new PropertyMetadata(string.Empty, OnValueChanged));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(string),
            typeof(KPIValue),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DisplayTextProperty =
        DependencyProperty.Register(
            nameof(DisplayText),
            typeof(string),
            typeof(KPIValue),
            new PropertyMetadata(string.Empty, OnDisplayTextChanged));

    public static readonly DependencyProperty IsMonetaryProperty =
        DependencyProperty.Register(
            nameof(IsMonetary),
            typeof(bool),
            typeof(KPIValue),
            new PropertyMetadata(false, OnIsMonetaryChanged));

    public static readonly DependencyProperty IsRevealedProperty =
        DependencyProperty.Register(
            nameof(IsRevealed),
            typeof(bool),
            typeof(KPIValue),
            new PropertyMetadata(false));

    public static readonly DependencyProperty RenderedTextProperty =
        DependencyProperty.Register(
            nameof(RenderedText),
            typeof(string),
            typeof(KPIValue),
            new PropertyMetadata(string.Empty));

    private static readonly DependencyProperty CountProperty =
        DependencyProperty.Register(
            "Count",
            typeof(double),
            typeof(KPIValue),
            new PropertyMetadata(0.0, OnCountChanged));

    private string _prefix = string.Empty;
    private string _suffix = string.Empty;
    private bool _useThousandsSeparator;
    private int _decimalPlaces;
    private DispatcherTimer? _autoHideTimer;

    public KPIValue()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>The text actually computed for Value - either mid count-up-animation or the final real Value. Read-only from a consumer's perspective. Not necessarily what's on screen; see RenderedText.</summary>
    public string DisplayText => (string)GetValue(DisplayTextProperty);

    /// <summary>Whether Value is a monetary amount that should be masked until the user reveals it. Non-monetary counts (customers, bookings, tasks) leave this false and behave exactly as before.</summary>
    public bool IsMonetary
    {
        get => (bool)GetValue(IsMonetaryProperty);
        set => SetValue(IsMonetaryProperty, value);
    }

    /// <summary>Whether the masked amount is currently shown. Read-only from a consumer's perspective; toggled by clicking the eye button.</summary>
    public bool IsRevealed => (bool)GetValue(IsRevealedProperty);

    /// <summary>What's actually shown on screen: DisplayText, or a fixed-width mask while a monetary value is hidden. This is the one thing ValueText.Text binds to.</summary>
    public string RenderedText => (string)GetValue(RenderedTextProperty);

    private double Count
    {
        get => (double)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((KPIValue)d).AnimateToValue((string?)e.NewValue ?? string.Empty);

    private static void OnDisplayTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((KPIValue)d).UpdateRenderedText();

    private static void OnIsMonetaryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (KPIValue)d;
        self.RevealToggle.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        self.UpdateRenderedText();
    }

    private void AnimateToValue(string value)
    {
        if (!KpiNumberParsing.TryParse(value, out var target, out _prefix, out _suffix, out _useThousandsSeparator, out _decimalPlaces))
        {
            BeginAnimation(CountProperty, null);
            SetValue(DisplayTextProperty, value);
            return;
        }

        // UX audit fix: a From=0 To=0 animation has no motion to play, and
        // was leaving DisplayText at its empty-string default instead of
        // "0" - every KPI badge whose real value is exactly zero (a brand-
        // new, still-empty organization's very first look at Accounting/
        // Inventory/Specialists) rendered as a blank pill. There is nothing
        // to animate in this case anyway, so writing the formatted zero
        // directly is both the fix and a reasonable simplification.
        if (target == 0)
        {
            BeginAnimation(CountProperty, null);
            SetValue(CountProperty, 0.0);
            SetValue(DisplayTextProperty, FormatCount(0));
            return;
        }

        var animation = new DoubleAnimation
        {
            From = 0,
            To = target,
            Duration = TimeSpan.FromMilliseconds(700),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        BeginAnimation(CountProperty, animation);
    }

    private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (KPIValue)d;
        self.SetValue(DisplayTextProperty, self.FormatCount((double)e.NewValue));
    }

    private string FormatCount(double count)
    {
        var pattern = _useThousandsSeparator ? "#,0" : "0";
        if (_decimalPlaces > 0)
        {
            pattern += "." + new string('0', _decimalPlaces);
        }

        return _prefix + count.ToString(pattern, CultureInfo.InvariantCulture) + _suffix;
    }

    private void UpdateRenderedText() =>
        SetValue(RenderedTextProperty, IsMonetary && !IsRevealed ? MaskGlyph : DisplayText);

    private void RevealToggle_Click(object sender, RoutedEventArgs e)
    {
        if (IsRevealed)
        {
            Hide();
        }
        else
        {
            Reveal();
        }
    }

    private void Reveal()
    {
        SetValue(IsRevealedProperty, true);
        UpdateRenderedText();

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        ValueText.BeginAnimation(OpacityProperty, new DoubleAnimation(0.35, 1, RevealDuration) { EasingFunction = easing });
        ValueScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.92, 1, RevealDuration) { EasingFunction = easing });
        ValueScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.92, 1, RevealDuration) { EasingFunction = easing });

        _autoHideTimer?.Stop();
        _autoHideTimer = new DispatcherTimer { Interval = AutoHideDelay };
        _autoHideTimer.Tick += AutoHideTimer_Tick;
        _autoHideTimer.Start();
    }

    private void AutoHideTimer_Tick(object? sender, EventArgs e) => Hide();

    private void Hide()
    {
        _autoHideTimer?.Stop();
        if (_autoHideTimer is not null)
        {
            _autoHideTimer.Tick -= AutoHideTimer_Tick;
            _autoHideTimer = null;
        }

        SetValue(IsRevealedProperty, false);
        UpdateRenderedText();

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        ValueText.BeginAnimation(OpacityProperty, new DoubleAnimation(0.35, 1, RevealDuration) { EasingFunction = easing });
        ValueScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.96, 1, RevealDuration) { EasingFunction = easing });
        ValueScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.96, 1, RevealDuration) { EasingFunction = easing });
    }
}
