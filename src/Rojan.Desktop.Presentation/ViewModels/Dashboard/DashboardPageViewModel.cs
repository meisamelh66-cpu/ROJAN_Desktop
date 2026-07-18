using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Dashboard;
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
            new("New Booking"),
            new("Add Client"),
            new("Create Task"),
            new("View Reports"),
        };
        QuickActionCommand = new RelayCommand(_ => { });

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
