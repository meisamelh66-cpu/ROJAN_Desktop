using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Automation;

/// <summary>Requirement 32.5's multi-step Approval Workflow tab - Leave/Expense/Inventory/Branch, plus whatever a workflow's Approval step raised automatically. Deciding a request that resolves a workflow-originated one (<c>ApprovalRequestDto.WorkflowExecutionId</c> set) transparently resumes/fails that paused execution - see <c>ApprovalService.DecideAsync</c>.</summary>
public sealed class ApprovalsTabViewModel : ViewModelBase
{
    private readonly IApprovalService _approvalService;
    private readonly string _currentUserId;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private string _decisionComment = string.Empty;

    public ApprovalsTabViewModel(IApprovalService approvalService, string currentUserId)
    {
        _approvalService = approvalService;
        _currentUserId = currentUserId;

        Requests = [];

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        ApproveCommand = new AsyncRelayCommand(parameter => DecideAsync((ApprovalRequestDto)parameter!, approve: true));
        RejectCommand = new AsyncRelayCommand(parameter => DecideAsync((ApprovalRequestDto)parameter!, approve: false));
    }

    public ObservableCollection<ApprovalRequestDto> Requests { get; }

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

    public string DecisionComment
    {
        get => _decisionComment;
        set => SetProperty(ref _decisionComment, value);
    }

    public ICommand LoadCommand { get; }

    public ICommand ApproveCommand { get; }

    public ICommand RejectCommand { get; }

    public async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var requests = await _approvalService.GetAllAsync().ConfigureAwait(true);
            Requests.Clear();
            foreach (var request in requests)
            {
                Requests.Add(request);
            }

            State = Requests.Count == 0 ? DashboardState.Empty : DashboardState.Loaded;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
        }
    }

    private async Task DecideAsync(ApprovalRequestDto request, bool approve)
    {
        try
        {
            var comment = string.IsNullOrWhiteSpace(DecisionComment) ? null : DecisionComment.Trim();
            await _approvalService.DecideAsync(request.Id, approve, _currentUserId, comment, default).ConfigureAwait(true);
            DecisionComment = string.Empty;
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = exception.Message;
        }
    }
}
