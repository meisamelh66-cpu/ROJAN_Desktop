using DomainAutomation = Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Application.Automation;

/// <summary>Default <see cref="IWorkflowService"/>. Mints workflow ids/timestamps itself for anything created here, the same "caller supplies intent, service mints identity/timestamp" shape <c>Notifications.NotificationService</c> already established.</summary>
public sealed class WorkflowService : IWorkflowService
{
    private readonly DomainAutomation.IWorkflowRepository _repository;

    public WorkflowService(DomainAutomation.IWorkflowRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<WorkflowDefinitionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var workflows = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return workflows.OrderByDescending(workflow => workflow.UpdatedAt).Select(AutomationMapping.Map).ToList();
    }

    public async Task<WorkflowDefinitionDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var workflow = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return workflow is null ? null : AutomationMapping.Map(workflow);
    }

    public async Task<IReadOnlyList<WorkflowDefinitionDto>> GetVersionsAsync(string parentWorkflowId, CancellationToken cancellationToken = default)
    {
        var versions = await _repository.GetVersionsAsync(parentWorkflowId, cancellationToken).ConfigureAwait(false);
        return versions.OrderByDescending(workflow => workflow.Version).Select(AutomationMapping.Map).ToList();
    }

    public async Task<IReadOnlyList<WorkflowDefinitionDto>> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        var workflows = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return workflows
            .Where(workflow => workflow.Status == DomainAutomation.WorkflowStatus.Published && workflow.IsEnabled)
            .Select(AutomationMapping.Map)
            .ToList();
    }

    public IReadOnlyList<string> Validate(IReadOnlyList<WorkflowStepDto> steps) =>
        DomainAutomation.WorkflowRules.Validate(steps.Select(AutomationMapping.MapToDomain).ToList());

    public async Task<WorkflowDefinitionDto> CreateDraftAsync(
        string name,
        string description,
        IReadOnlyList<WorkflowStepDto> steps,
        IReadOnlyList<TriggerType> triggerTypes,
        string createdByUserId,
        string organizationId,
        string branchId,
        CancellationToken cancellationToken = default)
    {
        var id = NewId();
        var now = DateTimeOffset.UtcNow;
        var workflow = new DomainAutomation.WorkflowDefinition(
            id, id, name, description, steps.Select(AutomationMapping.MapToDomain).ToList(),
            triggerTypes.Select(AutomationMapping.MapToDomain).ToList(), DomainAutomation.WorkflowStatus.Draft,
            Version: 1, IsEnabled: true, now, now, createdByUserId, organizationId, branchId);

        await _repository.SaveAsync(workflow, cancellationToken).ConfigureAwait(false);
        return AutomationMapping.Map(workflow);
    }

    public async Task<WorkflowDefinitionDto> SaveDraftAsync(WorkflowDefinitionDto workflow, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(workflow.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Workflow '{workflow.Id}' was not found.");

        if (existing.Status != DomainAutomation.WorkflowStatus.Draft)
        {
            throw new InvalidOperationException($"Workflow '{workflow.Id}' is {existing.Status} - only a Draft can be edited.");
        }

        var updated = existing with
        {
            Name = workflow.Name,
            Description = workflow.Description,
            Steps = workflow.Steps.Select(AutomationMapping.MapToDomain).ToList(),
            TriggerTypes = workflow.TriggerTypes.Select(AutomationMapping.MapToDomain).ToList(),
            IsEnabled = workflow.IsEnabled,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return AutomationMapping.Map(updated);
    }

    public async Task<WorkflowDefinitionDto> PublishAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        var workflow = await _repository.GetByIdAsync(workflowId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Workflow '{workflowId}' was not found.");

        if (workflow.Status != DomainAutomation.WorkflowStatus.Draft)
        {
            throw new InvalidOperationException($"Workflow '{workflowId}' is {workflow.Status} - only a Draft can be published.");
        }

        var errors = DomainAutomation.WorkflowRules.Validate(workflow.Steps);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Workflow '{workflowId}' failed validation: {string.Join(" ", errors)}");
        }

        var versions = await _repository.GetVersionsAsync(workflow.ParentWorkflowId, cancellationToken).ConfigureAwait(false);
        var previouslyPublished = versions.FirstOrDefault(v => v.Status == DomainAutomation.WorkflowStatus.Published);
        if (previouslyPublished is not null)
        {
            await _repository.SaveAsync(previouslyPublished with { Status = DomainAutomation.WorkflowStatus.Archived, UpdatedAt = DateTimeOffset.UtcNow }, cancellationToken).ConfigureAwait(false);
        }

        var published = workflow with { Status = DomainAutomation.WorkflowStatus.Published, UpdatedAt = DateTimeOffset.UtcNow };
        await _repository.SaveAsync(published, cancellationToken).ConfigureAwait(false);
        return AutomationMapping.Map(published);
    }

    public async Task ArchiveAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        var workflow = await _repository.GetByIdAsync(workflowId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Workflow '{workflowId}' was not found.");

        await _repository.SaveAsync(workflow with { Status = DomainAutomation.WorkflowStatus.Archived, UpdatedAt = DateTimeOffset.UtcNow }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorkflowDefinitionDto> RollbackAsync(string parentWorkflowId, int toVersion, string userId, CancellationToken cancellationToken = default)
    {
        var versions = await _repository.GetVersionsAsync(parentWorkflowId, cancellationToken).ConfigureAwait(false);
        var target = versions.FirstOrDefault(v => v.Version == toVersion)
            ?? throw new InvalidOperationException($"Workflow lineage '{parentWorkflowId}' has no version {toVersion}.");

        var now = DateTimeOffset.UtcNow;
        var newDraft = target with
        {
            Id = NewId(),
            Status = DomainAutomation.WorkflowStatus.Draft,
            Version = versions.Max(v => v.Version) + 1,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = userId,
        };

        await _repository.SaveAsync(newDraft, cancellationToken).ConfigureAwait(false);
        return AutomationMapping.Map(newDraft);
    }

    public Task DeleteAsync(string workflowId, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(workflowId, cancellationToken);

    private static string NewId() => Guid.NewGuid().ToString("N");
}
