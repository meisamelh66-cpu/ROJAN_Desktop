using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>Which time range is currently selected on a KpiChart's TimeRangeSelector.</summary>
public enum TimeRange
{
    Day,
    Week,
    Month,
    Year,
    All,
}

/// <summary>
/// Five-option segmented time-range control - see TimeRangeSelector.xaml for
/// the full Phase 35 reasoning (real, functional selection state; no
/// historical data to actually filter by yet, so nothing downstream is
/// wired to SelectedRange today beyond exposing it for a future consumer).
/// </summary>
public partial class TimeRangeSelector : UserControl
{
    private static int _instanceCounter;

    public static readonly DependencyProperty SelectedRangeProperty =
        DependencyProperty.Register(
            nameof(SelectedRange),
            typeof(TimeRange),
            typeof(TimeRangeSelector),
            new PropertyMetadata(TimeRange.Month));

    public TimeRangeSelector()
    {
        InitializeComponent();

        // GroupName-based mutual exclusion in WPF is scoped by string across
        // the whole window, not by parent container - every TimeRangeSelector
        // instance needs its own unique group so selecting a range on one KPI
        // card can't uncheck the option on another card.
        var groupName = $"KpiTimeRange_{Interlocked.Increment(ref _instanceCounter)}";
        DayOption.GroupName = groupName;
        WeekOption.GroupName = groupName;
        MonthOption.GroupName = groupName;
        YearOption.GroupName = groupName;
        AllOption.GroupName = groupName;
    }

    public TimeRange SelectedRange
    {
        get => (TimeRange)GetValue(SelectedRangeProperty);
        private set => SetValue(SelectedRangeProperty, value);
    }

    private void Option_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tag } && Enum.TryParse<TimeRange>(tag, out var range))
        {
            SelectedRange = range;
        }
    }
}
