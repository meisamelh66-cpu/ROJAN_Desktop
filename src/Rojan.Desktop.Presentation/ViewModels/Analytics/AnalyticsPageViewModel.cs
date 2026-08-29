using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Reporting;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Analytics;

/// <summary>
/// Drives the Analytics Dashboard: a period switcher (Daily/Weekly/
/// Monthly, matching the three Dashboard reports' own periods), the KPI
/// Engine's eight cards, and three native-rendered charts (no external
/// charting library - see <c>Controls.Analytics.SimpleBarChart</c>).
/// </summary>
public sealed partial class AnalyticsPageViewModel : ViewModelBase
{
    private readonly IKpiEngineQueryService _kpiEngineQueryService;
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly ILogger<AnalyticsPageViewModel> _logger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private AnalyticsPeriod _selectedPeriod = AnalyticsPeriod.Daily;
    private AnalyticsSummaryDto? _summary;

    public AnalyticsPageViewModel(IKpiEngineQueryService kpiEngineQueryService, IAnalyticsQueryService analyticsQueryService, ILogger<AnalyticsPageViewModel>? logger = null)
    {
        _kpiEngineQueryService = kpiEngineQueryService;
        _analyticsQueryService = analyticsQueryService;
        _logger = logger ?? NullLogger<AnalyticsPageViewModel>.Instance;

        Kpis = new ObservableCollection<KpiValueDto>();
        Charts = new ObservableCollection<ChartDefinitionDto>();

        SelectPeriodCommand = new RelayCommand(period => SelectedPeriod = (AnalyticsPeriod)period!);
        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());

        _ = LoadAsync();
    }

    public ObservableCollection<KpiValueDto> Kpis { get; }

    public ObservableCollection<ChartDefinitionDto> Charts { get; }

    public ICommand SelectPeriodCommand { get; }

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

    public AnalyticsPeriod SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            if (SetProperty(ref _selectedPeriod, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public AnalyticsSummaryDto? Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var kpisTask = _kpiEngineQueryService.GetKpisAsync(SelectedPeriod);
            var summaryTask = _analyticsQueryService.GetAnalyticsSummaryAsync(SelectedPeriod);
            var chartsTask = _analyticsQueryService.GetDashboardChartsAsync(SelectedPeriod);
            await Task.WhenAll(kpisTask, summaryTask, chartsTask).ConfigureAwait(true);

            Kpis.Clear();
            foreach (var kpi in await kpisTask.ConfigureAwait(true))
            {
                Kpis.Add(kpi);
            }

            Summary = await summaryTask.ConfigureAwait(true);

            Charts.Clear();
            foreach (var chart in await chartsTask.ConfigureAwait(true))
            {
                Charts.Add(chart);
            }

            State = DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception)
#pragma warning restore CA1031
        {
            ErrorMessage = Strings.Common_ActionFailedMessage;
            State = DashboardState.Error;
            LogOperationFailed(nameof(LoadAsync));
        }
    }

    // Security: logs the operation name only - never the exception, its message,
    // analytics data, or any backend response detail.
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Analytics page operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);
}
