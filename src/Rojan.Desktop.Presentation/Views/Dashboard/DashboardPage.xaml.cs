using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Views.Dashboard;

/// <summary>
/// Dashboard layout. DataContext is the resolved DashboardPageViewModel, set
/// by WPF's implicit DataTemplate resolution - unchanged. Dashboard
/// Modernization Sprint: adds a live, View-local greeting/date/time header
/// (GreetingText/DateText/TimeText dependency properties, driven by a
/// DispatcherTimer off the real system clock) - deliberately not on the
/// ViewModel, since it is pure presentation state with no repository/domain
/// meaning.
/// </summary>
public partial class DashboardPage : UserControl
{
    private readonly DispatcherTimer _clockTimer;

    public static readonly DependencyProperty GreetingTextProperty =
        DependencyProperty.Register(
            nameof(GreetingText),
            typeof(string),
            typeof(DashboardPage),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DateTextProperty =
        DependencyProperty.Register(
            nameof(DateText),
            typeof(string),
            typeof(DashboardPage),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TimeTextProperty =
        DependencyProperty.Register(
            nameof(TimeText),
            typeof(string),
            typeof(DashboardPage),
            new PropertyMetadata(string.Empty));

    public DashboardPage()
    {
        InitializeComponent();

        _clockTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _clockTimer.Tick += (_, _) => UpdateClock();

        Loaded += (_, _) =>
        {
            UpdateClock();
            _clockTimer.Start();
        };
        Unloaded += (_, _) => _clockTimer.Stop();
    }

    public string GreetingText
    {
        get => (string)GetValue(GreetingTextProperty);
        private set => SetValue(GreetingTextProperty, value);
    }

    public string DateText
    {
        get => (string)GetValue(DateTextProperty);
        private set => SetValue(DateTextProperty, value);
    }

    public string TimeText
    {
        get => (string)GetValue(TimeTextProperty);
        private set => SetValue(TimeTextProperty, value);
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;

        GreetingText = now.Hour switch
        {
            < 12 => Strings.Dashboard_Greeting_Morning,
            < 18 => Strings.Dashboard_Greeting_Afternoon,
            _ => Strings.Dashboard_Greeting_Evening,
        };

        DateText = now.ToString("D", CultureInfo.CurrentCulture);
        TimeText = now.ToString("t", CultureInfo.CurrentCulture);
    }
}
