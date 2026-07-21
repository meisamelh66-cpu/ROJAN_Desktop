using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>
/// Label + value pair for a single KPI reading, plus (Dashboard Modernization
/// Sprint) an optional icon badge and a count-up reveal animation for Value.
/// The count-up is a pure visual replay of the real, already-bound Value
/// string - it parses the leading numeric run (keeping any prefix/suffix,
/// e.g. a currency word), animates from 0 up to that real number, and
/// re-renders the exact original format on every tick. If Value isn't a
/// plain formatted number the pattern can safely parse, it's shown
/// immediately as-is, unanimated - never a fabricated intermediate shape.
/// </summary>
public partial class KPIValue : UserControl
{
    private static readonly Regex LeadingNumberPattern =
        new(@"^(?<prefix>[^\d]*)(?<number>[\d,]+(?:\.[\d]+)?)(?<suffix>.*)$", RegexOptions.Compiled);

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

    /// <summary>The text actually rendered - either mid count-up-animation or the final real Value. Read-only from a consumer's perspective.</summary>
    public string DisplayText => (string)GetValue(DisplayTextProperty);

    private double Count
    {
        get => (double)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((KPIValue)d).AnimateToValue((string?)e.NewValue ?? string.Empty);

    private void AnimateToValue(string value)
    {
        var match = LeadingNumberPattern.Match(value);
        var numberText = match.Success ? match.Groups["number"].Value : string.Empty;

        if (!match.Success || !double.TryParse(numberText, NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var target))
        {
            BeginAnimation(CountProperty, null);
            SetValue(DisplayTextProperty, value);
            return;
        }

        _prefix = match.Groups["prefix"].Value;
        _suffix = match.Groups["suffix"].Value;
        _useThousandsSeparator = numberText.Contains(',');
        _decimalPlaces = numberText.Contains('.') ? numberText[(numberText.IndexOf('.') + 1)..].Length : 0;

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
        var count = (double)e.NewValue;
        var pattern = self._useThousandsSeparator ? "#,0" : "0";
        if (self._decimalPlaces > 0)
        {
            pattern += "." + new string('0', self._decimalPlaces);
        }

        self.SetValue(DisplayTextProperty, self._prefix + count.ToString(pattern, CultureInfo.InvariantCulture) + self._suffix);
    }
}
