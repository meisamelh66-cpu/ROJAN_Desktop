using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Automation;

/// <summary>
/// Requirement 32.1 (Workflow Designer, architecture-only - "Prepare for
/// future drag-and-drop designer" explicitly defers a visual step editor)
/// plus 32.9 (Versioning). "Create" builds a minimal, immediately valid
/// Start-&gt;End skeleton rather than opening a step editor that doesn't
/// exist yet - every other Requirement 32 capability (publish/archive/
/// rollback/execute/trigger-subscription) is still fully real and
/// exercisable against it.
/// </summary>
public sealed partial class WorkflowsTabViewModel : ViewModelBase
{
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowExecutionEngine _executionEngine;
    private readonly string _currentUserId;
    private readonly string _organizationId;
    private readonly string _branchId;
    private readonly ILogger<WorkflowsTabViewModel> _logger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private string _newWorkflowName = string.Empty;
    private string _newWorkflowDescription = string.Empty;
    private TriggerType? _newWorkflowTrigger;
    private WorkflowDefinitionDto? _selectedWorkflow;

    public WorkflowsTabViewModel(IWorkflowService workflowService, IWorkflowExecutionEngine executionEngine, string currentUserId, string organizationId, string branchId, ILogger<WorkflowsTabViewModel>? logger = null)
    {
        _workflowService = workflowService;
        _executionEngine = executionEngine;
        _currentUserId = currentUserId;
        _organizationId = organizationId;
        _branchId = branchId;
        _logger = logger ?? NullLogger<WorkflowsTabViewModel>.Instance;

        Workflows = [];
        VersionHistory = [];
        AvailableTriggerTypes = Enum.GetValues<TriggerType>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        CreateDraftCommand = new AsyncRelayCommand(_ => CreateDraftAsync(), _ => !string.IsNullOrWhiteSpace(NewWorkflowName));
        PublishCommand = new AsyncRelayCommand(parameter => PublishAsync((WorkflowDefinitionDto)parameter!));
        ArchiveCommand = new AsyncRelayCommand(parameter => ArchiveAsync((WorkflowDefinitionDto)parameter!));
        DeleteCommand = new AsyncRelayCommand(parameter => DeleteAsync((WorkflowDefinitionDto)parameter!));
        RunNowCommand = new AsyncRelayCommand(parameter => RunNowAsync((WorkflowDefinitionDto)parameter!));
        RollbackCommand = new AsyncRelayCommand(parameter => RollbackAsync((WorkflowDefinitionDto)parameter!));
    }

    public ObservableCollection<WorkflowDefinitionDto> Workflows { get; }

    public ObservableCollection<WorkflowDefinitionDto> VersionHistory { get; }

    public IReadOnlyList<TriggerType> AvailableTriggerTypes { get; }

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

    public string NewWorkflowName
    {
        get => _newWorkflowName;
        set
        {
            if (SetProperty(ref _newWorkflowName, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string NewWorkflowDescription
    {
        get => _newWorkflowDescription;
        set => SetProperty(ref _newWorkflowDescription, value);
    }

    public TriggerType? NewWorkflowTrigger
    {
        get => _newWorkflowTrigger;
        set => SetProperty(ref _newWorkflowTrigger, value);
    }

    public WorkflowDefinitionDto? SelectedWorkflow
    {
        get => _selectedWorkflow;
        set
        {
            if (SetProperty(ref _selectedWorkflow, value))
            {
                _ = LoadVersionHistoryAsync();
            }
        }
    }

    public ICommand LoadCommand { get; }

    public ICommand CreateDraftCommand { get; }

    public ICommand PublishCommand { get; }

    public ICommand ArchiveCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand RunNowCommand { get; }

    public ICommand RollbackCommand { get; }

    public async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var workflows = await _workflowService.GetAllAsync().ConfigureAwait(true);
            Workflows.Clear();
            foreach (var workflow in workflows)
            {
                Workflows.Add(workflow);
            }

            State = Workflows.Count == 0 ? DashboardState.Empty : DashboardState.Loaded;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
            LogOperationFailed(nameof(LoadAsync));
        }
    }

    private async Task LoadVersionHistoryAsync()
    {
        try
        {
            VersionHistory.Clear();
            if (SelectedWorkflow is null)
            {
                return;
            }

            var versions = await _workflowService.GetVersionsAsync(SelectedWorkflow.ParentWorkflowId).ConfigureAwait(true);
            foreach (var version in versions)
            {
                VersionHistory.Add(version);
            }

            ErrorMessage = null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = Localization.Strings.Common_ActionFailedMessage;
            LogOperationFailed(nameof(LoadVersionHistoryAsync));
        }
    }

    private async Task CreateDraftAsync()
    {
        try
        {
            var steps = BuildDefaultSteps();
            var triggers = NewWorkflowTrigger is { } trigger ? new List<TriggerType> { trigger } : [];
            await _workflowService.CreateDraftAsync(NewWorkflowName.Trim(), NewWorkflowDescription.Trim(), steps, triggers, _currentUserId, _organizationId, _branchId).ConfigureAwait(true);

            NewWorkflowName = string.Empty;
            NewWorkflowDescription = string.Empty;
            NewWorkflowTrigger = null;
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = exception.Message;
            LogOperationFailed(nameof(CreateDraftAsync));
        }
    }

    private static IReadOnlyList<WorkflowStepDto> BuildDefaultSteps()
    {
        var startId = Guid.NewGuid().ToString("N");
        var endId = Guid.NewGuid().ToString("N");
        return
        [
            new WorkflowStepDto(startId, WorkflowStepType.Start, "Start", new Dictionary<string, string>(), endId, null),
            new WorkflowStepDto(endId, WorkflowStepType.End, "End", new Dictionary<string, string>(), null, null),
        ];
    }

    private async Task PublishAsync(WorkflowDefinitionDto workflow)
    {
        try
        {
            await _workflowService.PublishAsync(workflow.Id).ConfigureAwait(true);
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = exception.Message;
            LogOperationFailed(nameof(PublishAsync));
        }
    }

    private async Task ArchiveAsync(WorkflowDefinitionDto workflow)
    {
        try
        {
            await _workflowService.ArchiveAsync(workflow.Id).ConfigureAwait(true);
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = Localization.Strings.Common_ActionFailedMessage;
            LogOperationFailed(nameof(ArchiveAsync));
        }
    }

    private async Task DeleteAsync(WorkflowDefinitionDto workflow)
    {
        try
        {
            await _workflowService.DeleteAsync(workflow.Id).ConfigureAwait(true);
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = Localization.Strings.Common_ActionFailedMessage;
            LogOperationFailed(nameof(DeleteAsync));
        }
    }

    private async Task RunNowAsync(WorkflowDefinitionDto workflow)
    {
        try
        {
            await _executionEngine.ExecuteAsync(workflow.Id, null, _currentUserId, new Dictionary<string, string>(), _organizationId, _branchId).ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = exception.Message;
            LogOperationFailed(nameof(RunNowAsync));
        }
    }

    private async Task RollbackAsync(WorkflowDefinitionDto workflow)
    {
        try
        {
            await _workflowService.RollbackAsync(workflow.ParentWorkflowId, workflow.Version, _currentUserId).ConfigureAwait(true);
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = exception.Message;
            LogOperationFailed(nameof(RollbackAsync));
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Automation workflows operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);
}
