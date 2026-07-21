namespace Rojan.Desktop.Application.Automation;

/// <summary>Scheduled Jobs CRUD plus due-job execution (Requirement 32.4) - Hourly/Daily/Weekly/Monthly, Cron-ready. <see cref="GetDueJobsAsync"/>/<see cref="RunDueJobAsync"/> are what <c>Infrastructure.Automation.WorkflowSchedulerService</c>'s background timer calls.</summary>
public interface IScheduledJobService
{
    public Task<IReadOnlyList<ScheduledJobDto>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task<ScheduledJobDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    public Task<ScheduledJobDto> CreateAsync(string name, ScheduleFrequency frequency, string? cronExpression, string workflowId, string organizationId, string branchId, CancellationToken cancellationToken = default);

    public Task<ScheduledJobDto> UpdateAsync(ScheduledJobDto job, CancellationToken cancellationToken = default);

    public Task SetEnabledAsync(string id, bool isEnabled, CancellationToken cancellationToken = default);

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Every enabled job whose <see cref="ScheduledJobDto.NextRunAt"/> has passed.</summary>
    public Task<IReadOnlyList<ScheduledJobDto>> GetDueJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>Executes the job's linked workflow, then advances <see cref="ScheduledJobDto.NextRunAt"/>/<see cref="ScheduledJobDto.LastRunAt"/>.</summary>
    public Task<WorkflowExecutionDto> RunDueJobAsync(string jobId, CancellationToken cancellationToken = default);
}
