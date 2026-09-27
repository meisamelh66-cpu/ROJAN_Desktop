using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Rojan.Desktop.Application.Dashboard;
using Rojan.Desktop.Presentation.Controls.Dashboard;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Navigation;
using Rojan.Desktop.Presentation.ViewModels.AI;
using Rojan.Desktop.Presentation.ViewModels.Analytics;
using Rojan.Desktop.Presentation.ViewModels.Bookings;
using Rojan.Desktop.Presentation.ViewModels.Customers;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.HR;
using Rojan.Desktop.Presentation.ViewModels.Inventory;
using Rojan.Desktop.Presentation.ViewModels.Reporting;
using Rojan.Desktop.Presentation.ViewModels.Services;

namespace Rojan.Desktop.Presentation.Views.Dashboard;

/// <summary>
/// Dashboard layout. DataContext is the resolved DashboardPageViewModel, set
/// by WPF's implicit DataTemplate resolution - unchanged. Dashboard Layout
/// Cleanup: the former greeting/date/time header (and its clock timer) and
/// the top New Booking button were removed - the admin-managed hero banner is
/// now the first content element. UpdateResponsiveLayout keeps the single
/// dashboard grid readable at any width (see its own comment).
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
/// UX Improvements - Dashboard Layout: the three Analytics
/// Row charts (SalonHealthChart_Click/TopServicesChart_Click/
/// RevenueTrendChart_Click) are new real navigation, following this same
/// "real navigation lives here, not on the ViewModel's no-op *Command
/// properties" precedent - wired via the standard bubbling Button.Click
/// event from the clickable-chart Button each card now wraps its chart
/// in (see Rojan.Style.ClickableChart's own doc comment), attached
/// directly on each card's tag in DashboardPage.xaml.
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

    /// <summary>Below this Dashboard content width the card rows restack to one card per line.</summary>
    public const double CompactWidth = 900;

    private const double CardGap = 4;

    private DashboardPageViewModel? _subscribedViewModel;

    public static readonly DependencyProperty KpiColumnsProperty =
        DependencyProperty.Register(
            nameof(KpiColumns),
            typeof(int),
            typeof(DashboardPage),
            new PropertyMetadata(3));

    public static readonly DependencyProperty NewsTickerItemsProperty =
        DependencyProperty.Register(
            nameof(NewsTickerItems),
            typeof(IEnumerable),
            typeof(DashboardPage),
            new PropertyMetadata(null));

    public DashboardPage()
    {
        InitializeComponent();

        // Place the cards in their wide-layout columns before the first layout pass; the real width
        // arrives with DashboardLayout's first SizeChanged.
        UpdateResponsiveLayout(CompactWidth);

        DataContextChanged += OnDataContextChanged;
    }

    public IEnumerable? NewsTickerItems
    {
        get => (IEnumerable?)GetValue(NewsTickerItemsProperty);
        private set => SetValue(NewsTickerItemsProperty, value);
    }

    /// <summary>Column count of the KPI card grid - see <see cref="ComputeKpiColumns"/>.</summary>
    public int KpiColumns
    {
        get => (int)GetValue(KpiColumnsProperty);
        private set => SetValue(KpiColumnsProperty, value);
    }

    /// <summary>
    /// KPI grid columns for a given Dashboard content width: 6 (one row) on wide screens, 3 (two
    /// even rows of the six cards) on typical laptops, then 2 and 1 - always a divisor of six so the
    /// last row is never a lone card. Public and static so it is testable without a WPF control.
    /// </summary>
    public static int ComputeKpiColumns(double width) => width switch
    {
        >= 1500 => 6,
        >= 720 => 3,
        >= 480 => 2,
        _ => 1,
    };

    /// <summary>True when the three-column card rows should restack to one card per line.</summary>
    public static bool IsCompactWidth(double width) => width < CompactWidth;

    private void DashboardLayout_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.WidthChanged)
        {
            UpdateResponsiveLayout(e.NewSize.Width);
        }
    }

    // Keeps the single Dashboard grid readable at any width: the KPI grid picks its column count,
    // and each three-column card row (OperationalRowA/B, AnalyticsRow) either places its cards in
    // columns 0/2/4 (1* / 1* / 1.4*, with the 4px gap columns between) or, below CompactWidth,
    // stacks them full width in reading order with the same 4px gap.
    private void UpdateResponsiveLayout(double width)
    {
        KpiColumns = ComputeKpiColumns(width);

        var compact = IsCompactWidth(width);
        foreach (var row in new[] { OperationalRowA, OperationalRowB, AnalyticsRow })
        {
            var index = 0;
            foreach (FrameworkElement card in row.Children)
            {
                Grid.SetColumn(card, compact ? 0 : index * 2);
                Grid.SetColumnSpan(card, compact ? row.ColumnDefinitions.Count : 1);
                Grid.SetRow(card, compact ? index : 0);
                card.Margin = new Thickness(0, compact && index > 0 ? CardGap : 0, 0, 0);
                index++;
            }
        }
    }

    private void QuickActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: QuickActionItem item })
        {
            return;
        }

        // New Booking no longer appears in this list (see DashboardPageViewModel's
        // own doc comment).
        if (item.Label == Strings.Dashboard_QuickAction_AddClient)
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

    /// <summary>UX Improvements - Dashboard Layout: Salon Health's chart click target - see Rojan.Style.ClickableChart's own doc comment.</summary>
    private void SalonHealthChart_Click(object sender, RoutedEventArgs e) => NavigateOrShowComingSoon<AnalyticsPageViewModel>();

    /// <summary>UX Improvements - Dashboard Layout: Top Services' chart click target - see Rojan.Style.ClickableChart's own doc comment.</summary>
    private void TopServicesChart_Click(object sender, RoutedEventArgs e) => NavigateOrShowComingSoon<ServicePageViewModel>();

    /// <summary>UX Improvements - Dashboard Layout: Revenue Trend's chart click target - see Rojan.Style.ClickableChart's own doc comment.</summary>
    private void RevenueTrendChart_Click(object sender, RoutedEventArgs e) => NavigateOrShowComingSoon<ReportingPageViewModel>();

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
