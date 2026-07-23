using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Input;
using Rojan.Desktop.Application.Dashboard;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Dashboard;

/// <summary>
/// Drives DashboardPage. Loads real (fake-repository-backed) data through
/// <see cref="IDashboardQueryService"/> - the only Application dependency
/// this ViewModel has, consistent with Presentation never reaching past
/// Application into Domain/Infrastructure. Quick Actions remain static
/// display data here: they are UI affordances (buttons that trigger future
/// commands), not fetched data, so they have no repository/DTO of their
/// own.
/// </summary>
public sealed class DashboardPageViewModel : ViewModelBase
{
    private static readonly CompositeFormat HeroCtaFormat = CompositeFormat.Parse(Strings.Dashboard_Hero_CtaFormat);

    private readonly IDashboardQueryService _queryService;
    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;

    public DashboardPageViewModel(IDashboardQueryService queryService)
    {
        _queryService = queryService;

        KpiMetrics = new ObservableCollection<KpiMetricDto>();
        RecentActivity = new ObservableCollection<ActivityEntryDto>();

        QuickActions = new ObservableCollection<QuickActionItem>
        {
            new(Strings.Dashboard_QuickAction_NewBooking),
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

            KpiMetrics.Clear();
            foreach (var metric in overview.KpiMetrics)
            {
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
        }
    }
}
