using Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Application.Tests.Automation;

/// <summary>In-memory <see cref="IWorkflowRepository"/> test double, same "mutable in-memory state, no real I/O" shape every other Fake*Repository in this codebase uses.</summary>
internal sealed class FakeWorkflowRepository : IWorkflowRepository
{
    private readonly List<WorkflowDefinition> _workflows = [];

    public Task<IReadOnlyList<WorkflowDefinition>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkflowDefinition>>(_workflows.ToList());

    public Task<WorkflowDefinition?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_workflows.FirstOrDefault(workflow => workflow.Id == id));

    public Task<IReadOnlyList<WorkflowDefinition>> GetVersionsAsync(string parentWorkflowId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkflowDefinition>>(_workflows.Where(workflow => workflow.ParentWorkflowId == parentWorkflowId).ToList());

    public Task SaveAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default)
    {
        var index = _workflows.FindIndex(existing => existing.Id == workflow.Id);
        if (index >= 0)
        {
            _workflows[index] = workflow;
        }
        else
        {
            _workflows.Add(workflow);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _workflows.RemoveAll(workflow => workflow.Id == id);
        return Task.CompletedTask;
    }
}

internal sealed class FakeBusinessRuleRepository : IBusinessRuleRepository
{
    private readonly List<BusinessRule> _rules = [];

    public Task<IReadOnlyList<BusinessRule>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BusinessRule>>(_rules.ToList());

    public Task<BusinessRule?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_rules.FirstOrDefault(rule => rule.Id == id));

    public Task SaveAsync(BusinessRule rule, CancellationToken cancellationToken = default)
    {
        var index = _rules.FindIndex(existing => existing.Id == rule.Id);
        if (index >= 0)
        {
            _rules[index] = rule;
        }
        else
        {
            _rules.Add(rule);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _rules.RemoveAll(rule => rule.Id == id);
        return Task.CompletedTask;
    }
}

internal sealed class FakeScheduledJobRepository : IScheduledJobRepository
{
    private readonly List<ScheduledJob> _jobs = [];

    public Task<IReadOnlyList<ScheduledJob>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ScheduledJob>>(_jobs.ToList());

    public Task<ScheduledJob?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_jobs.FirstOrDefault(job => job.Id == id));

    public Task SaveAsync(ScheduledJob job, CancellationToken cancellationToken = default)
    {
        var index = _jobs.FindIndex(existing => existing.Id == job.Id);
        if (index >= 0)
        {
            _jobs[index] = job;
        }
        else
        {
            _jobs.Add(job);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _jobs.RemoveAll(job => job.Id == id);
        return Task.CompletedTask;
    }
}

internal sealed class FakeApprovalRepository : IApprovalRepository
{
    private readonly List<ApprovalRequest> _requests = [];

    public Task<IReadOnlyList<ApprovalRequest>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ApprovalRequest>>(_requests.ToList());

    public Task<ApprovalRequest?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_requests.FirstOrDefault(request => request.Id == id));

    public Task SaveAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        var index = _requests.FindIndex(existing => existing.Id == request.Id);
        if (index >= 0)
        {
            _requests[index] = request;
        }
        else
        {
            _requests.Add(request);
        }

        return Task.CompletedTask;
    }
}

internal sealed class FakeWorkflowExecutionRepository : IWorkflowExecutionRepository
{
    private readonly List<WorkflowExecution> _executions = [];

    public Task<IReadOnlyList<WorkflowExecution>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkflowExecution>>(_executions.ToList());

    public Task<WorkflowExecution?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_executions.FirstOrDefault(execution => execution.Id == id));

    public Task SaveAsync(WorkflowExecution execution, CancellationToken cancellationToken = default)
    {
        var index = _executions.FindIndex(existing => existing.Id == execution.Id);
        if (index >= 0)
        {
            _executions[index] = execution;
        }
        else
        {
            _executions.Add(execution);
        }

        return Task.CompletedTask;
    }
}
