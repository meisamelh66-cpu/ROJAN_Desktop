using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Navigation;
using Rojan.Desktop.Presentation.ViewModels.Bookings;
using Rojan.Desktop.Presentation.ViewModels.Customers;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Reporting;

namespace Rojan.Desktop.Presentation.Views.Dashboard;

/// <summary>
/// Dashboard layout. DataContext is the resolved DashboardPageViewModel, set
/// by WPF's implicit DataTemplate resolution - unchanged. Dashboard
/// Modernization Sprint: adds a live, View-local greeting/date/time header
/// (GreetingText/DateText/TimeText dependency properties, driven by a
/// DispatcherTimer off the real system clock) - deliberately not on the
/// ViewModel, since it is pure presentation state with no repository/domain
/// meaning.
///
/// Bug fix: Quick Action buttons now really navigate. DashboardPageViewModel's
/// QuickActionCommand is an intentional no-op and off-limits (ViewModels
/// can't be modified for this fix), so this is a plain Click handler instead
/// - it never touches the ViewModel, only reads the clicked item's Label and
/// asks DashboardNavigationBridge (a Presentation-layer static bridge Shell
/// populates once at startup - see that class's own doc comment) to
/// navigate to the matching existing page, or shows a "coming soon" message
/// for the one action (Create Task) with no real destination yet.
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

    private void QuickActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: QuickActionItem item })
        {
            return;
        }

        if (item.Label == Strings.Dashboard_QuickAction_NewBooking)
        {
            NavigateOrShowComingSoon<BookingPageViewModel>();
        }
        else if (item.Label == Strings.Dashboard_QuickAction_AddClient)
        {
            NavigateOrShowComingSoon<CustomerPageViewModel>();
        }
        else if (item.Label == Strings.Dashboard_QuickAction_ViewReports)
        {
            NavigateOrShowComingSoon<ReportingPageViewModel>();
        }
        else
        {
            // "Create Task": no Tasks module exists anywhere in the app yet - honest
            // "coming soon" rather than navigating nowhere or doing nothing.
            ShowComingSoon();
        }
    }

    private static void NavigateOrShowComingSoon<TViewModel>() where TViewModel : ViewModelBase
    {
        var navigationService = DashboardNavigationBridge.Current;
        if (navigationService is null)
        {
            ShowComingSoon();
            return;
        }

        navigationService.NavigateTo<TViewModel>();
    }

    private static void ShowComingSoon() =>
        MessageBox.Show(Strings.Dashboard_ComingSoon, Strings.Dashboard_QuickActions, MessageBoxButton.OK, MessageBoxImage.Information);
}
