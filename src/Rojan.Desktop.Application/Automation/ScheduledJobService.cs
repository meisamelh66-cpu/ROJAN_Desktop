using DomainAutomation = Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Application.Automation;

/// <summary>Default <see cref="IScheduledJobService"/>. A job's initial <see cref="ScheduledJobDto.NextRunAt"/> is computed from the moment it's created; each run recomputes it from that run's own start time (not from a fixed origin), so a job that's been disabled for a while resumes on a fresh cadence rather than immediately catching up on every missed interval.</summary>
public sealed class ScheduledJobService : IScheduledJobService
{
    private readonly DomainAutomation.IScheduledJobRepository _repository;
    private readonly IWorkflowExecutionEngine _executionEngine;

    public ScheduledJobService(DomainAutomation.IScheduledJobRepository repository, IWorkflowExecutionEngine executionEngine)
    {
        _repository = repository;
        _executionEngine = executionEngine;
    }

    public async Task<IReadOnlyList<ScheduledJobDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return jobs.OrderBy(job => job.NextRunAt).Select(AutomationMapping.Map).ToList();
    }

    public async Task<ScheduledJobDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var job = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return job is null ? null : AutomationMapping.Map(job);
    }

    public async Task<ScheduledJobDto> CreateAsync(string name, ScheduleFrequency frequency, string? cronExpression, string workflowId, string organizationId, string branchId, CancellationToken cancellationToken = default)
    {
        var domainFrequency = AutomationMapping.MapToDomain(frequency);
        var now = DateTimeOffset.UtcNow;
        var job = new DomainAutomation.ScheduledJob(
            Guid.NewGuid().ToString("N"), name, domainFrequency, cronExpression, workflowId, IsEnabled: true,
            DomainAutomation.ScheduleRules.ComputeNextRun(domainFrequency, now), LastRunAt: null, organizationId, branchId);

        await _repository.SaveAsync(job, cancellationToken).ConfigureAwait(false);
        return AutomationMapping.Map(job);
    }

    public async Task<ScheduledJobDto> UpdateAsync(ScheduledJobDto job, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(job.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Scheduled job '{job.Id}' was not found.");

        var updated = existing with
        {
            Name = job.Name,
            Frequency = AutomationMapping.MapToDomain(job.Frequency),
            CronExpression = job.CronExpression,
            WorkflowId = job.WorkflowId,
            IsEnabled = job.IsEnabled,
        };

        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return AutomationMapping.Map(updated);
    }

    public async Task SetEnabledAsync(string id, bool isEnabled, CancellationToken cancellationToken = default)
    {
        var job = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Scheduled job '{id}' was not found.");

        await _repository.SaveAsync(job with { IsEnabled = isEnabled }, cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(id, cancellationToken);

    public async Task<IReadOnlyList<ScheduledJobDto>> GetDueJobsAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        return jobs.Where(job => DomainAutomation.ScheduleRules.IsDue(job, now)).Select(AutomationMapping.Map).ToList();
    }

    public async Task<WorkflowExecutionDto> RunDueJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var job = await _repository.GetByIdAsync(jobId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Scheduled job '{jobId}' was not found.");

        var now = DateTimeOffset.UtcNow;
        var execution = await _executionEngine.ExecuteAsync(
            job.WorkflowId, trigger: null, triggeredByUserId: "scheduler", facts: new Dictionary<string, string>(),
            job.OrganizationId, job.BranchId, cancellationToken).ConfigureAwait(false);

        var updatedJob = job with { LastRunAt = now, NextRunAt = DomainAutomation.ScheduleRules.ComputeNextRun(job.Frequency, now) };
        await _repository.SaveAsync(updatedJob, cancellationToken).ConfigureAwait(false);

        return execution;
    }
}
