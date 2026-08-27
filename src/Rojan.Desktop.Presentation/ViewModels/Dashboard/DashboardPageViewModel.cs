using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Dashboard;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Reporting;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Organizations;

namespace Rojan.Desktop.Presentation.ViewModels.Dashboard;

/// <summary>
/// Drives DashboardPage. Loads real (fake-repository-backed) data through
/// <see cref="IDashboardQueryService"/> - the only Application dependency
/// this ViewModel has, consistent with Presentation never reaching past
/// Application into Domain/Infrastructure. Quick Actions remain static
/// display data here: they are UI affordances (buttons that trigger future
/// commands), not fetched data, so they have no repository/DTO of their
/// own.
///
/// Reception Stabilization Sprint: <see cref="LoadAsync"/> now filters out
/// the <see cref="FinancialKpiId"/> tile (real backend revenue) for any
/// role lacking <see cref="Permission.AccountingView"/> - previously every
/// role, including Reception (which <c>RolePermissions</c> never grants
/// that permission), saw real salon revenue on first login with no
/// filtering anywhere in this ViewModel. Reuses the existing permission -
/// no new one added.
/// </summary>
public sealed partial class DashboardPageViewModel : ViewModelBase
{
    private const string FinancialKpiId = "kpi-revenue";

    private static readonly CompositeFormat HeroCtaFormat = CompositeFormat.Parse(Strings.Dashboard_Hero_CtaFormat);

    private readonly IDashboardQueryService _queryService;
    private readonly IPermissionEngine _permissionEngine;
    private readonly ICurrentSessionService _currentSessionService;
    private readonly ILogger<DashboardPageViewModel> _logger;
    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;

    public DashboardPageViewModel(IDashboardQueryService queryService, IPermissionEngine permissionEngine, ICurrentSessionService currentSessionService, ILogger<DashboardPageViewModel>? logger = null)
    {
        _queryService = queryService;
        _permissionEngine = permissionEngine;
        _currentSessionService = currentSessionService;
        _logger = logger ?? NullLogger<DashboardPageViewModel>.Instance;

        KpiMetrics = new ObservableCollection<KpiMetricDto>();
        RecentActivity = new ObservableCollection<ActivityEntryDto>();

        // UX Improvements - Dashboard Layout: New Booking removed from this
        // list - promoted to its own prominent top-of-page primary action
        // button instead (see DashboardPage.xaml/.xaml.cs's own comments),
        // not left duplicated in both places.
        QuickActions = new ObservableCollection<QuickActionItem>
        {
            new(Strings.Dashboard_QuickAction_AddClient),
            new(Strings.Dashboard_QuickAction_CreateTask),
            new(Strings.Dashboard_QuickAction_ViewReports),
        };
        QuickActionCommand = new RelayCommand(_ => { });

        // Phase B-1 (AI Hero Banner): mock, presentation-only content - no
        // repository/DTO of its own, same reasoning QuickActions already
        // documents above. Both commands are intentional no-ops, same
        // "Phase 06B placeholder, no business logic yet" precedent
        // QuickActionCommand already sets.
        HeroTagLabel = Strings.Dashboard_Hero_TagLabel;
        HeroHeadline = Strings.Dashboard_Hero_Headline;
        HeroSubtitle = Strings.Dashboard_Hero_Subtitle;
        HeroCtaLabel = string.Format(CultureInfo.CurrentCulture, HeroCtaFormat, 3);
        HeroViewSuggestionsCommand = new RelayCommand(_ => { });
        HeroSecondaryCommand = new RelayCommand(_ => { });

        // Phase C-1 (Analytics Row): mock, presentation-only chart data -
        // same "no repository/DTO of its own" reasoning as QuickActions/
        // Hero Banner above. ChartDefinitionDto/ChartSeriesDto are the
        // existing Application-layer types SimpleBarChart (Controls/
        // Analytics) already renders elsewhere in the app - reused as-is,
        // not a new chart-data shape. Title is left blank on all three:
        // this page builds its own icon+title header per card (see
        // RevenueTrendCard/TopServicesCard/SalonHealthCard), so
        // SimpleBarChart's own internal title line would otherwise
        // duplicate it.
        // Reuses the existing Organizations_* day-of-week strings (already
        // localized fa/en/ar for the working-hours schedule) rather than
        // declaring a second set of day names.
        DayLabels =
        [
            Strings.Organizations_Saturday,
            Strings.Organizations_Sunday,
            Strings.Organizations_Monday,
            Strings.Organizations_Tuesday,
            Strings.Organizations_Wednesday,
            Strings.Organizations_Thursday,
            Strings.Organizations_Friday,
        ];
        RevenueTrendChart = new ChartDefinitionDto(
            "dashboard-revenue-trend",
            string.Empty,
            ChartType.Area,
            DayLabels,
            [new ChartSeriesDto("Revenue", [8_000_000m, 11_000_000m, 13_500_000m, 12_000_000m, 14_000_000m, 17_400_000m, 15_500_000m])]);

        TopServicesChart = new ChartDefinitionDto(
            "dashboard-top-services",
            string.Empty,
            ChartType.Bar,
            [
                Strings.Dashboard_Service_HairCutStyle,
                Strings.Dashboard_Service_RootColorTouchUp,
                Strings.Dashboard_Service_KeratinStraightening,
                Strings.Dashboard_Service_Manicure,
                Strings.Dashboard_Service_Massage,
            ],
            [new ChartSeriesDto("Value", [5m, 4m, 3m, 2m, 1m])]);
        ViewAllServicesCommand = new RelayCommand(_ => { });

        SalonHealthScore = 92;
        SalonHealthScoreText = SalonHealthScore.ToString(CultureInfo.CurrentCulture);
        SalonHealthStatusLabel = Strings.Dashboard_Analytics_HealthStatusExcellent;
        SalonHealthSubtitle = Strings.Dashboard_Analytics_HealthSubtitle;
        SalonHealthChart = new ChartDefinitionDto(
            "dashboard-salon-health",
            string.Empty,
            ChartType.Donut,
            [Strings.Dashboard_Analytics_HealthStatusExcellent, string.Empty],
            [new ChartSeriesDto("Health", [SalonHealthScore, 100 - SalonHealthScore])]);
        SalonHealthDetailsCommand = new RelayCommand(_ => { });

        // Phase C-2 (Staff Status Panel / Recent Alerts Panel): mock,
        // presentation-only lists - same "no repository of its own"
        // reasoning as QuickActions/Hero Banner/Analytics Row above. Names
        // reuse FakeSpecialistRepository's real seed names (consistency
        // with the rest of the app, not fabricated identities); workload
        // percentages/remaining-time text are mock since no real
        // scheduling/workload data source exists yet.
        var minutesFormat = CompositeFormat.Parse(Strings.Dashboard_Staff_MinutesRemainingFormat);
        StaffStatuses =
        [
            new StaffStatusItem { Name = "کیانا رادمنش", ServiceLabel = Strings.Dashboard_Staff_Service_HairCut, IsOnline = true, ProgressPercentage = 65, RemainingTimeText = string.Format(CultureInfo.CurrentCulture, minutesFormat, 25) },
            new StaffStatusItem { Name = "سارا امینی", ServiceLabel = Strings.Dashboard_Staff_Service_ColorHighlight, IsOnline = true, ProgressPercentage = 80, RemainingTimeText = string.Format(CultureInfo.CurrentCulture, minutesFormat, 10) },
            new StaffStatusItem { Name = "مهسا کریمی", ServiceLabel = Strings.Dashboard_Staff_Service_NailArt, IsOnline = true, ProgressPercentage = 40, RemainingTimeText = string.Format(CultureInfo.CurrentCulture, minutesFormat, 45) },
            new StaffStatusItem { Name = "نیلوفر صفایی", ServiceLabel = Strings.Dashboard_Staff_Service_Keratin, IsOnline = false, ProgressPercentage = 100, RemainingTimeText = null },
            new StaffStatusItem { Name = "پویا احمدپور", ServiceLabel = Strings.Dashboard_Staff_Service_ColorHighlight, IsOnline = false, ProgressPercentage = 0, RemainingTimeText = null },
        ];
        ViewAllStaffCommand = new RelayCommand(_ => { });

        RecentAlerts =
        [
            new AlertItem { Icon = "Rojan.Icon.Calendar2", Text = Strings.Dashboard_Alert_LeaveRequest },
            new AlertItem { Icon = "Rojan.Icon.Warning", Text = Strings.Dashboard_Alert_InventoryLow },
            new AlertItem { Icon = "Rojan.Icon.Accounting", Text = Strings.Dashboard_Alert_PaymentReceived },
        ];

        // Phase C-2 (final part, Daily Schedule): mock, presentation-only
        // list - same "no repository of its own" reasoning as the lists
        // above. Reuses the same specialist names as the Staff Status
        // Panel for consistency (the same people appear in both views).
        ScheduleItems =
        [
            new ScheduleItem { Time = "۰۹:۳۰", SpecialistName = "کیانا رادمنش", ServiceLabel = Strings.Dashboard_Staff_Service_HairCut, IsCurrent = false },
            new ScheduleItem { Time = "۱۰:۴۵", SpecialistName = "سارا امینی", ServiceLabel = Strings.Dashboard_Staff_Service_ColorHighlight, IsCurrent = false },
            new ScheduleItem { Time = "۱۲:۳۰", SpecialistName = "مهسا کریمی", ServiceLabel = Strings.Dashboard_Staff_Service_NailArt, IsCurrent = true },
            new ScheduleItem { Time = "۱۴:۱۵", SpecialistName = "نیلوفر صفایی", ServiceLabel = Strings.Dashboard_Staff_Service_Keratin, IsCurrent = false },
            new ScheduleItem { Time = "۱۵:۳۰", SpecialistName = "پویا احمدپور", ServiceLabel = Strings.Dashboard_Staff_Service_ColorHighlight, IsCurrent = false },
        ];
        ViewFullScheduleCommand = new RelayCommand(_ => { });

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, so there is nothing
        // left that could become an unobserved task exception.
        _ = LoadAsync();
    }

    public ObservableCollection<KpiMetricDto> KpiMetrics { get; }

    public ObservableCollection<ActivityEntryDto> RecentActivity { get; }

    public ObservableCollection<QuickActionItem> QuickActions { get; }

    /// <summary>Bound by every Quick Action button. Intentionally a no-op - Phase 06B is still layout/architecture, no business logic.</summary>
    public ICommand QuickActionCommand { get; }

    public string HeroTagLabel { get; }

    public string HeroHeadline { get; }

    public string HeroSubtitle { get; }

    public string HeroCtaLabel { get; }

    /// <summary>Bound by the Hero Banner's primary CTA. Intentionally a no-op - same "Phase 06B placeholder, no business logic yet" reasoning as <see cref="QuickActionCommand"/>.</summary>
    public ICommand HeroViewSuggestionsCommand { get; }

    /// <summary>Bound by the Hero Banner's secondary icon button. Intentionally a no-op - same reasoning as <see cref="HeroViewSuggestionsCommand"/>.</summary>
    public ICommand HeroSecondaryCommand { get; }

    public IReadOnlyList<string> DayLabels { get; }

    public ChartDefinitionDto RevenueTrendChart { get; }

    public ChartDefinitionDto TopServicesChart { get; }

    /// <summary>Bound by Top Services' "View All Services" button (disabled). Intentionally a no-op - same reasoning as <see cref="HeroViewSuggestionsCommand"/>.</summary>
    public ICommand ViewAllServicesCommand { get; }

    public int SalonHealthScore { get; }

    public string SalonHealthScoreText { get; }

    public string SalonHealthStatusLabel { get; }

    public string SalonHealthSubtitle { get; }

    public ChartDefinitionDto SalonHealthChart { get; }

    /// <summary>Bound by Salon Health's "Score Details" button (disabled). Intentionally a no-op - same reasoning as <see cref="HeroViewSuggestionsCommand"/>.</summary>
    public ICommand SalonHealthDetailsCommand { get; }

    public IReadOnlyList<StaffStatusItem> StaffStatuses { get; }

    /// <summary>Bound by Staff Status Panel's "View All Specialists" button (disabled). Intentionally a no-op - same reasoning as <see cref="HeroViewSuggestionsCommand"/>.</summary>
    public ICommand ViewAllStaffCommand { get; }

    public IReadOnlyList<AlertItem> RecentAlerts { get; }

    public IReadOnlyList<ScheduleItem> ScheduleItems { get; }

    /// <summary>Bound by Daily Schedule's "View Full Schedule" button (disabled). Intentionally a no-op - same reasoning as <see cref="HeroViewSuggestionsCommand"/>.</summary>
    public ICommand ViewFullScheduleCommand { get; }

    /// <summary>Re-runs the load - bound as the Retry action on DashboardWidget's Error state.</summary>
    public ICommand LoadCommand { get; }

    public DashboardState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var overview = await _queryService.GetOverviewAsync().ConfigureAwait(true);
            var canViewFinancials = _permissionEngine.HasPermission(_currentSessionService.CurrentRole, Permission.AccountingView);

            KpiMetrics.Clear();
            foreach (var metric in overview.KpiMetrics)
            {
                if (metric.Id == FinancialKpiId && !canViewFinancials)
                {
                    continue;
                }

                KpiMetrics.Add(metric);
            }

            RecentActivity.Clear();
            foreach (var activity in overview.RecentActivity)
            {
                RecentActivity.Add(activity);
            }

            State = KpiMetrics.Count == 0 && RecentActivity.Count == 0
                ? DashboardState.Empty
                : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - this is the one place a broad catch is the correct behavior, not a code smell.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
            LogLoadFailed(exception);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Dashboard overview load failed.")]
    private partial void LogLoadFailed(Exception exception);
}
