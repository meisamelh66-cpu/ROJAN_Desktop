using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Rojan.Desktop.Application.Dashboard;
using Rojan.Desktop.Presentation.Controls.Dashboard;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Navigation;
using Rojan.Desktop.Presentation.ViewModels.AI;
using Rojan.Desktop.Presentation.ViewModels.Bookings;
using Rojan.Desktop.Presentation.ViewModels.Customers;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.HR;
using Rojan.Desktop.Presentation.ViewModels.Inventory;
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
///
/// Phase 36 (Live News Ticker): NewsTickerItems mixes real entries (reusing
/// the same RecentActivity ids ActivityDescriptionConverter already maps,
/// plus a real KpiMetrics trend fact) with demo entries for the categories
/// the sprint's spec calls for that have no real data source anywhere in
/// the app yet (birthdays, inventory warnings, staff attendance, AI
/// recommendations, etc.) - demo content is explicitly authorized by that
/// spec's own "If data is unavailable, show Demo Data only" clause. Rebuilt
/// whenever RecentActivity/KpiMetrics change (DashboardPageViewModel's
/// LoadAsync populates them asynchronously after construction), the same
/// live-reactivity lesson the Bug 3 fix (DashboardKpiCollection) learned -
/// a one-time snapshot would miss the real data arriving.
/// </summary>
public partial class DashboardPage : UserControl
{
    private static readonly CompositeFormat ActiveClientsTrendFormat = CompositeFormat.Parse(Strings.News_ActiveClientsTrend);

    private readonly DispatcherTimer _clockTimer;
    private DashboardPageViewModel? _subscribedViewModel;

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

    public static readonly DependencyProperty NewsTickerItemsProperty =
        DependencyProperty.Register(
            nameof(NewsTickerItems),
            typeof(IEnumerable),
            typeof(DashboardPage),
            new PropertyMetadata(null));

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

        DataContextChanged += OnDataContextChanged;
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

    public IEnumerable? NewsTickerItems
    {
        get => (IEnumerable?)GetValue(NewsTickerItemsProperty);
        private set => SetValue(NewsTickerItemsProperty, value);
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

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.RecentActivity.CollectionChanged -= OnDashboardDataChanged;
            _subscribedViewModel.KpiMetrics.CollectionChanged -= OnDashboardDataChanged;
            _subscribedViewModel = null;
        }

        if (e.NewValue is DashboardPageViewModel viewModel)
        {
            viewModel.RecentActivity.CollectionChanged += OnDashboardDataChanged;
            viewModel.KpiMetrics.CollectionChanged += OnDashboardDataChanged;
            _subscribedViewModel = viewModel;
            RebuildNewsTicker(viewModel);
        }
    }

    private void OnDashboardDataChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_subscribedViewModel is not null)
        {
            RebuildNewsTicker(_subscribedViewModel);
        }
    }

    private void RebuildNewsTicker(DashboardPageViewModel viewModel)
    {
        var items = new List<NewsTickerItem>();

        foreach (var activity in viewModel.RecentActivity)
        {
            NewsTickerItem? item = activity.Id switch
            {
                "activity-1" => new NewsTickerItem { Icon = ResolveIcon("Rojan.Icon.Bookings"), Text = Strings.Dashboard_Activity_NewBooking, OnClick = () => NavigateOrShowComingSoon<BookingPageViewModel>() },
                "activity-2" => new NewsTickerItem { Icon = ResolveIcon("Rojan.Icon.Customers"), Text = Strings.Dashboard_Activity_ProfileUpdated, OnClick = () => NavigateOrShowComingSoon<CustomerPageViewModel>() },
                "activity-3" => new NewsTickerItem { Icon = ResolveIcon("Rojan.Icon.Accounting"), Text = Strings.Dashboard_Activity_PaymentReceived, OnClick = ShowComingSoon },
                "activity-4" => new NewsTickerItem { Icon = ResolveIcon("Rojan.Icon.CheckCircle"), Text = Strings.Dashboard_Activity_TaskCompleted, OnClick = ShowComingSoon },
                _ => null,
            };

            if (item is not null)
            {
                items.Add(item);
            }
        }

        var clients = viewModel.KpiMetrics.FirstOrDefault(m => m.Id == "kpi-clients");
        if (clients is not null)
        {
            var trendIcon = clients.TrendDirection == TrendDirection.Down ? "Rojan.Icon.TrendDown" : "Rojan.Icon.TrendUp";
            items.Add(new NewsTickerItem
            {
                Icon = ResolveIcon(trendIcon),
                Text = string.Format(CultureInfo.CurrentCulture, ActiveClientsTrendFormat, clients.TrendPercentage.ToString("0.0", CultureInfo.CurrentCulture)),
                OnClick = () => NavigateOrShowComingSoon<CustomerPageViewModel>(),
            });
        }

        // Demo entries - explicitly authorized fallback for the categories this
        // sprint requires that have no real data source anywhere in the app yet.
        items.Add(new NewsTickerItem { Icon = ResolveIcon("Rojan.Icon.Dismiss"), Text = Strings.News_Demo_AppointmentCancelled, OnClick = () => NavigateOrShowComingSoon<BookingPageViewModel>() });
        items.Add(new NewsTickerItem { Icon = ResolveIcon("Rojan.Icon.FavoriteFilled"), Text = Strings.News_Demo_CustomerBirthday, OnClick = () => NavigateOrShowComingSoon<CustomerPageViewModel>(), IsLive = true });
        items.Add(new NewsTickerItem { Icon = ResolveIcon("Rojan.Icon.Reports"), Text = Strings.News_Demo_RevenueSummary, OnClick = () => NavigateOrShowComingSoon<ReportingPageViewModel>() });
        items.Add(new NewsTickerItem { Icon = ResolveIcon("Rojan.Icon.Warning"), Text = Strings.News_Demo_InventoryWarning, OnClick = () => NavigateOrShowComingSoon<InventoryPageViewModel>() });
        items.Add(new NewsTickerItem { Icon = ResolveIcon("Rojan.Icon.Specialists"), Text = Strings.News_Demo_StaffAttendance, OnClick = () => NavigateOrShowComingSoon<HrPageViewModel>() });
        items.Add(new NewsTickerItem { Icon = ResolveIcon("Rojan.Icon.AICenter"), Text = Strings.News_Demo_AiRecommendation, OnClick = () => NavigateOrShowComingSoon<AiCenterPageViewModel>() });
        items.Add(new NewsTickerItem { Icon = ResolveIcon("Rojan.Icon.Information"), Text = Strings.News_Demo_SystemNotification, OnClick = ShowComingSoon });

        NewsTickerItems = items;
    }

    private static string ResolveIcon(string key) =>
        System.Windows.Application.Current.TryFindResource(key) as string ?? string.Empty;
}
