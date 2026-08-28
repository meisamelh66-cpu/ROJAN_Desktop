using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Automation;

/// <summary>Requirement 32.12's Automation Dashboard tab: workflow count, today's executions, failures, success rate, average execution time, plus a recent-executions strip (Requirement 32.8's monitoring).</summary>
public sealed partial class AutomationDashboardTabViewModel : ViewModelBase
{
    private readonly IAutomationDashboardQueryService _dashboardQueryService;
    private readonly IWorkflowExecutionEngine _executionEngine;
    private readonly ILogger<AutomationDashboardTabViewModel> _logger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private int _totalWorkflows;
    private int _publishedWorkflows;
    private int _executionsToday;
    private int _failuresToday;
    private double _successRatePercent;
    private double _averageExecutionDurationMs;
    private int _pendingApprovals;

    public AutomationDashboardTabViewModel(IAutomationDashboardQueryService dashboardQueryService, IWorkflowExecutionEngine executionEngine, ILogger<AutomationDashboardTabViewModel>? logger = null)
    {
        _dashboardQueryService = dashboardQueryService;
        _executionEngine = executionEngine;
        _logger = logger ?? NullLogger<AutomationDashboardTabViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        RecentExecutions = [];
    }

    public ICommand LoadCommand { get; }

    public ObservableCollection<WorkflowExecutionDto> RecentExecutions { get; }

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

    public int TotalWorkflows
    {
        get => _totalWorkflows;
        private set => SetProperty(ref _totalWorkflows, value);
    }

    public int PublishedWorkflows
    {
        get => _publishedWorkflows;
        private set => SetProperty(ref _publishedWorkflows, value);
    }

    public int ExecutionsToday
    {
        get => _executionsToday;
        private set => SetProperty(ref _executionsToday, value);
    }

    public int FailuresToday
    {
        get => _failuresToday;
        private set => SetProperty(ref _failuresToday, value);
    }

    public double SuccessRatePercent
    {
        get => _successRatePercent;
        private set => SetProperty(ref _successRatePercent, value);
    }

    public double AverageExecutionDurationMs
    {
        get => _averageExecutionDurationMs;
        private set => SetProperty(ref _averageExecutionDurationMs, value);
    }

    public int PendingApprovals
    {
        get => _pendingApprovals;
        private set => SetProperty(ref _pendingApprovals, value);
    }

    public async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var summary = await _dashboardQueryService.GetSummaryAsync().ConfigureAwait(true);
            TotalWorkflows = summary.TotalWorkflows;
            PublishedWorkflows = summary.PublishedWorkflows;
            ExecutionsToday = summary.ExecutionsToday;
            FailuresToday = summary.FailuresToday;
            SuccessRatePercent = summary.SuccessRatePercent;
            AverageExecutionDurationMs = summary.AverageExecutionDurationMs;
            PendingApprovals = summary.PendingApprovals;

            var history = await _executionEngine.GetHistoryAsync().ConfigureAwait(true);
            RecentExecutions.Clear();
            foreach (var execution in history.Take(10))
            {
                RecentExecutions.Add(execution);
            }

            State = DashboardState.Loaded;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
            LogOperationFailed(nameof(LoadAsync));
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Automation dashboard operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);
}
