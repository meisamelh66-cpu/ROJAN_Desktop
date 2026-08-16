using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Presentation.Organizations;

namespace Rojan.Desktop.Presentation.Tests.Automation;

/// <summary>Minimal <see cref="ICurrentSessionService"/> test double - the Automation tab ViewModels only read <see cref="CurrentOrganization"/>/<see cref="CurrentBranch"/>/<see cref="CurrentRole"/> once at construction, so nothing here needs to actually change live.</summary>
internal sealed class FakeCurrentSessionService : ICurrentSessionService
{
    public OrganizationDto? CurrentOrganization => null;

    public BranchDto? CurrentBranch => null;

    public WorkspaceRole CurrentRole => WorkspaceRole.PlatformOwner;

    public bool HasRealMembership => false;

    public IReadOnlyList<BranchDto> AvailableBranches => [];

    public IReadOnlyList<string> RecentBranchIds => [];

    public IReadOnlyList<string> FavoriteBranchIds => [];

    public event EventHandler? SessionChanged { add { } remove { } }

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SwitchBranchAsync(string branchId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SwitchRoleAsync(WorkspaceRole role, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ToggleFavoriteBranchAsync(string branchId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>In-memory <see cref="IWorkflowService"/> test double - just enough Draft/Published/Archived/rollback bookkeeping to exercise <c>WorkflowsTabViewModel</c>'s wiring; the real versioning rules are already covered by Application.Tests.</summary>
internal sealed class StubWorkflowService : IWorkflowService
{
    private readonly List<WorkflowDefinitionDto> _workflows = [];

    public IReadOnlyList<string> ValidateErrors { get; set; } = [];

    public Task<IReadOnlyList<WorkflowDefinitionDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkflowDefinitionDto>>(_workflows.ToList());

    public Task<WorkflowDefinitionDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_workflows.FirstOrDefault(w => w.Id == id));

    public Task<IReadOnlyList<WorkflowDefinitionDto>> GetVersionsAsync(string parentWorkflowId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkflowDefinitionDto>>(_workflows.Where(w => w.ParentWorkflowId == parentWorkflowId).OrderByDescending(w => w.Version).ToList());

    public Task<IReadOnlyList<WorkflowDefinitionDto>> GetPublishedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkflowDefinitionDto>>(_workflows.Where(w => w.Status == WorkflowStatus.Published && w.IsEnabled).ToList());

    public IReadOnlyList<string> Validate(IReadOnlyList<WorkflowStepDto> steps) => ValidateErrors;

    public Task<WorkflowDefinitionDto> CreateDraftAsync(
        string name, string description, IReadOnlyList<WorkflowStepDto> steps, IReadOnlyList<TriggerType> triggerTypes,
        string createdByUserId, string organizationId, string branchId, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;
        var workflow = new WorkflowDefinitionDto(id, id, name, description, steps, triggerTypes, WorkflowStatus.Draft, 1, true, now, now, createdByUserId, organizationId, branchId);
        _workflows.Add(workflow);
        return Task.FromResult(workflow);
    }

    public Task<WorkflowDefinitionDto> SaveDraftAsync(WorkflowDefinitionDto workflow, CancellationToken cancellationToken = default)
    {
        Replace(workflow);
        return Task.FromResult(workflow);
    }

    public Task<WorkflowDefinitionDto> PublishAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        var published = Get(workflowId) with { Status = WorkflowStatus.Published };
        Replace(published);
        return Task.FromResult(published);
    }

    public Task ArchiveAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        Replace(Get(workflowId) with { Status = WorkflowStatus.Archived });
        return Task.CompletedTask;
    }

    public Task<WorkflowDefinitionDto> RollbackAsync(string parentWorkflowId, int toVersion, string userId, CancellationToken cancellationToken = default)
    {
        var target = _workflows.First(w => w.ParentWorkflowId == parentWorkflowId && w.Version == toVersion);
        var newVersion = _workflows.Where(w => w.ParentWorkflowId == parentWorkflowId).Max(w => w.Version) + 1;
        var draft = target with { Id = Guid.NewGuid().ToString("N"), Status = WorkflowStatus.Draft, Version = newVersion, CreatedByUserId = userId };
        _workflows.Add(draft);
        return Task.FromResult(draft);
    }

    public Task DeleteAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        _workflows.RemoveAll(w => w.Id == workflowId);
        return Task.CompletedTask;
    }

    private WorkflowDefinitionDto Get(string id) => _workflows.First(w => w.Id == id);

    private void Replace(WorkflowDefinitionDto workflow) => _workflows[_workflows.FindIndex(w => w.Id == workflow.Id)] = workflow;
}

/// <summary>In-memory <see cref="IBusinessRuleService"/> test double - just enough CRUD to exercise <c>BusinessRulesTabViewModel</c>'s wiring.</summary>
internal sealed class StubBusinessRuleService : IBusinessRuleService
{
    private readonly List<BusinessRuleDto> _rules = [];

    public Task<IReadOnlyList<BusinessRuleDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BusinessRuleDto>>(_rules.ToList());

    public Task<BusinessRuleDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_rules.FirstOrDefault(r => r.Id == id));

    public Task<BusinessRuleDto> CreateAsync(
        string name, string description, IReadOnlyList<BusinessRuleConditionDto> conditions, BusinessRuleActionDto action,
        int priority, string organizationId, string branchId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var rule = new BusinessRuleDto(Guid.NewGuid().ToString("N"), name, description, conditions, action, priority, true, now, now, organizationId, branchId);
        _rules.Add(rule);
        return Task.FromResult(rule);
    }

    public Task<BusinessRuleDto> UpdateAsync(BusinessRuleDto rule, CancellationToken cancellationToken = default)
    {
        _rules[_rules.FindIndex(r => r.Id == rule.Id)] = rule;
        return Task.FromResult(rule);
    }

    public Task SetEnabledAsync(string id, bool isEnabled, CancellationToken cancellationToken = default)
    {
        var index = _rules.FindIndex(r => r.Id == id);
        _rules[index] = _rules[index] with { IsEnabled = isEnabled };
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _rules.RemoveAll(r => r.Id == id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BusinessRuleDto>> EvaluateAsync(IReadOnlyDictionary<string, string> facts, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BusinessRuleDto>>([]);

    public Task<IReadOnlyList<BusinessRuleDto>> ExecuteMatchingRulesAsync(IReadOnlyDictionary<string, string> facts, string organizationId, string branchId, string triggeredByUserId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BusinessRuleDto>>([]);
}

/// <summary>In-memory <see cref="IScheduledJobService"/> test double - just enough CRUD to exercise <c>ScheduledJobsTabViewModel</c>'s wiring.</summary>
internal sealed class StubScheduledJobService : IScheduledJobService
{
    private readonly List<ScheduledJobDto> _jobs = [];

    public Func<string, WorkflowExecutionDto>? RunDueJobResultFactory { get; set; }

    public Task<IReadOnlyList<ScheduledJobDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ScheduledJobDto>>(_jobs.ToList());

    public Task<ScheduledJobDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_jobs.FirstOrDefault(j => j.Id == id));

    public Task<ScheduledJobDto> CreateAsync(string name, ScheduleFrequency frequency, string? cronExpression, string workflowId, string organizationId, string branchId, CancellationToken cancellationToken = default)
    {
        var job = new ScheduledJobDto(Guid.NewGuid().ToString("N"), name, frequency, cronExpression, workflowId, true, DateTimeOffset.UtcNow.AddHours(1), null, organizationId, branchId);
        _jobs.Add(job);
        return Task.FromResult(job);
    }

    public Task<ScheduledJobDto> UpdateAsync(ScheduledJobDto job, CancellationToken cancellationToken = default)
    {
        _jobs[_jobs.FindIndex(j => j.Id == job.Id)] = job;
        return Task.FromResult(job);
    }

    public Task SetEnabledAsync(string id, bool isEnabled, CancellationToken cancellationToken = default)
    {
        var index = _jobs.FindIndex(j => j.Id == id);
        _jobs[index] = _jobs[index] with { IsEnabled = isEnabled };
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _jobs.RemoveAll(j => j.Id == id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ScheduledJobDto>> GetDueJobsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ScheduledJobDto>>(_jobs.ToList());

    public Task<WorkflowExecutionDto> RunDueJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var result = RunDueJobResultFactory?.Invoke(jobId) ?? new WorkflowExecutionDto(
            Guid.NewGuid().ToString("N"), jobId, 1, "Flow", WorkflowExecutionStatus.Completed, null, "scheduler", [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 5, null, "org-1", "branch-1");
        return Task.FromResult(result);
    }
}

/// <summary>In-memory <see cref="IApprovalService"/> test double - just enough CRUD/decide to exercise <c>ApprovalsTabViewModel</c>'s wiring.</summary>
internal sealed class StubApprovalService : IApprovalService
{
    private readonly List<ApprovalRequestDto> _requests = [];

    public void Seed(ApprovalRequestDto request) => _requests.Add(request);

    public Task<IReadOnlyList<ApprovalRequestDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ApprovalRequestDto>>(_requests.ToList());

    public Task<ApprovalRequestDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_requests.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<ApprovalRequestDto>> GetPendingForRoleAsync(string approverRole, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ApprovalRequestDto>>(_requests.Where(r => r.Status == ApprovalStatus.Pending).ToList());

    public Task<ApprovalRequestDto> CreateAsync(
        ApprovalType type, string title, string description, IReadOnlyList<string> approverRoles,
        string requestedByUserId, string organizationId, string branchId, CancellationToken cancellationToken = default)
    {
        var steps = approverRoles.Select((role, index) => new ApprovalStepDto(index, role, ApprovalStepStatus.Pending, null, null, null)).ToList();
        var request = new ApprovalRequestDto(
            Guid.NewGuid().ToString("N"), type, title, description, requestedByUserId, DateTimeOffset.UtcNow,
            steps, ApprovalStatus.Pending, 0, null, organizationId, branchId);
        _requests.Add(request);
        return Task.FromResult(request);
    }

    public string? LastDecidedRequestId { get; private set; }

    public string? LastDecisionComment { get; private set; }

    public Task<ApprovalRequestDto> DecideAsync(string requestId, bool approve, string userId, string? comment, CancellationToken cancellationToken = default)
    {
        LastDecidedRequestId = requestId;
        LastDecisionComment = comment;
        var index = _requests.FindIndex(r => r.Id == requestId);
        var decided = _requests[index] with { Status = approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected };
        _requests[index] = decided;
        return Task.FromResult(decided);
    }
}

/// <summary>In-memory <see cref="IWorkflowExecutionEngine"/> test double - just enough to exercise <c>WorkflowsTabViewModel.RunNowCommand</c> and <c>AutomationDashboardTabViewModel</c>'s recent-executions strip.</summary>
internal sealed class StubWorkflowExecutionEngine : IWorkflowExecutionEngine
{
    private readonly List<WorkflowExecutionDto> _executions = [];

    public int ExecuteCallCount { get; private set; }

    public Task<WorkflowExecutionDto> ExecuteAsync(string workflowId, TriggerType? trigger, string triggeredByUserId, IReadOnlyDictionary<string, string> facts, string organizationId, string branchId, CancellationToken cancellationToken = default)
    {
        ExecuteCallCount++;
        var execution = new WorkflowExecutionDto(Guid.NewGuid().ToString("N"), workflowId, 1, "Flow", WorkflowExecutionStatus.Completed, trigger, triggeredByUserId, [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 5, null, organizationId, branchId);
        _executions.Insert(0, execution);
        return Task.FromResult(execution);
    }

    public Task<IReadOnlyList<WorkflowExecutionDto>> GetHistoryAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkflowExecutionDto>>(_executions.ToList());

    public Task<WorkflowExecutionDto?> GetByIdAsync(string executionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_executions.FirstOrDefault(e => e.Id == executionId));

    public Task<WorkflowExecutionDto> ResumeApprovalAsync(string executionId, bool approved, CancellationToken cancellationToken = default)
    {
        var index = _executions.FindIndex(e => e.Id == executionId);
        var resumed = _executions[index] with { Status = approved ? WorkflowExecutionStatus.Completed : WorkflowExecutionStatus.Failed };
        _executions[index] = resumed;
        return Task.FromResult(resumed);
    }
}

/// <summary>Stub <see cref="IAutomationDashboardQueryService"/> test double returning a fixed, caller-supplied summary.</summary>
internal sealed class StubAutomationDashboardQueryService(AutomationDashboardSummaryDto summary) : IAutomationDashboardQueryService
{
    public Task<AutomationDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default) => Task.FromResult(summary);
}
