using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Automation;

/// <summary>Requirement 32.4's Scheduled Jobs tab - Hourly/Daily/Weekly/Monthly, Cron-ready. Actual due-job execution is driven by <c>Infrastructure.Automation.WorkflowSchedulerService</c>'s background timer, not by this ViewModel - <see cref="RunNowCommand"/> is a manual "run immediately" override for testing/urgency.</summary>
public sealed partial class ScheduledJobsTabViewModel : ViewModelBase
{
    private readonly IScheduledJobService _scheduledJobService;
    private readonly IWorkflowService _workflowService;
    private readonly string _organizationId;
    private readonly string _branchId;
    private readonly ILogger<ScheduledJobsTabViewModel> _logger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private string _newJobName = string.Empty;
    private ScheduleFrequency _newJobFrequency = ScheduleFrequency.Daily;
    private string _newJobCronExpression = string.Empty;
    private WorkflowDefinitionDto? _newJobWorkflow;

    public ScheduledJobsTabViewModel(IScheduledJobService scheduledJobService, IWorkflowService workflowService, string organizationId, string branchId, ILogger<ScheduledJobsTabViewModel>? logger = null)
    {
        _scheduledJobService = scheduledJobService;
        _workflowService = workflowService;
        _organizationId = organizationId;
        _branchId = branchId;
        _logger = logger ?? NullLogger<ScheduledJobsTabViewModel>.Instance;

        Jobs = [];
        PublishedWorkflows = [];
        AvailableFrequencies = Enum.GetValues<ScheduleFrequency>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        CreateCommand = new AsyncRelayCommand(_ => CreateAsync(), _ => !string.IsNullOrWhiteSpace(NewJobName) && NewJobWorkflow is not null);
        ToggleEnabledCommand = new AsyncRelayCommand(parameter => ToggleEnabledAsync((ScheduledJobDto)parameter!));
        DeleteCommand = new AsyncRelayCommand(parameter => DeleteAsync((ScheduledJobDto)parameter!));
        RunNowCommand = new AsyncRelayCommand(parameter => RunNowAsync((ScheduledJobDto)parameter!));
    }

    public ObservableCollection<ScheduledJobDto> Jobs { get; }

    public ObservableCollection<WorkflowDefinitionDto> PublishedWorkflows { get; }

    public IReadOnlyList<ScheduleFrequency> AvailableFrequencies { get; }

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

    public string NewJobName
    {
        get => _newJobName;
        set
        {
            if (SetProperty(ref _newJobName, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public ScheduleFrequency NewJobFrequency
    {
        get => _newJobFrequency;
        set => SetProperty(ref _newJobFrequency, value);
    }

    public string NewJobCronExpression
    {
        get => _newJobCronExpression;
        set => SetProperty(ref _newJobCronExpression, value);
    }

    public WorkflowDefinitionDto? NewJobWorkflow
    {
        get => _newJobWorkflow;
        set
        {
            if (SetProperty(ref _newJobWorkflow, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public ICommand LoadCommand { get; }

    public ICommand CreateCommand { get; }

    public ICommand ToggleEnabledCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand RunNowCommand { get; }

    public async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var jobs = await _scheduledJobService.GetAllAsync().ConfigureAwait(true);
            Jobs.Clear();
            foreach (var job in jobs)
            {
                Jobs.Add(job);
            }

            var published = await _workflowService.GetPublishedAsync().ConfigureAwait(true);
            PublishedWorkflows.Clear();
            foreach (var workflow in published)
            {
                PublishedWorkflows.Add(workflow);
            }

            State = Jobs.Count == 0 ? DashboardState.Empty : DashboardState.Loaded;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
            LogOperationFailed(nameof(LoadAsync));
        }
    }

    private async Task CreateAsync()
    {
        if (NewJobWorkflow is null)
        {
            return;
        }

        try
        {
            var cronExpression = NewJobFrequency == ScheduleFrequency.Cron ? NewJobCronExpression.Trim() : null;
            await _scheduledJobService.CreateAsync(NewJobName.Trim(), NewJobFrequency, cronExpression, NewJobWorkflow.Id, _organizationId, _branchId).ConfigureAwait(true);

            NewJobName = string.Empty;
            NewJobCronExpression = string.Empty;
            NewJobWorkflow = null;
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = exception.Message;
            LogOperationFailed(nameof(CreateAsync));
        }
    }

    private async Task ToggleEnabledAsync(ScheduledJobDto job)
    {
        try
        {
            await _scheduledJobService.SetEnabledAsync(job.Id, !job.IsEnabled).ConfigureAwait(true);
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = Localization.Strings.Common_ActionFailedMessage;
            LogOperationFailed(nameof(ToggleEnabledAsync));
        }
    }

    private async Task DeleteAsync(ScheduledJobDto job)
    {
        try
        {
            await _scheduledJobService.DeleteAsync(job.Id).ConfigureAwait(true);
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = Localization.Strings.Common_ActionFailedMessage;
            LogOperationFailed(nameof(DeleteAsync));
        }
    }

    private async Task RunNowAsync(ScheduledJobDto job)
    {
        try
        {
            await _scheduledJobService.RunDueJobAsync(job.Id).ConfigureAwait(true);
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = exception.Message;
            LogOperationFailed(nameof(RunNowAsync));
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Automation scheduled jobs operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);
}
