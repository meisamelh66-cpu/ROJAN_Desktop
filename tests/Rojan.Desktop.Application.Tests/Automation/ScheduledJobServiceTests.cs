using Rojan.Desktop.Application.Automation;

namespace Rojan.Desktop.Application.Tests.Automation;

/// <summary>Exercises <see cref="ScheduledJobService"/> - Requirement 32.4's Hourly/Daily/Weekly/Monthly scheduling, due-job selection, and running a due job.</summary>
public sealed class ScheduledJobServiceTests
{
    private static async Task<(FakeWorkflowRepository Workflows, string WorkflowId)> SeedPublishedWorkflowAsync()
    {
        var workflows = new FakeWorkflowRepository();
        var workflowService = new WorkflowService(workflows);
        var startId = Guid.NewGuid().ToString("N");
        var endId = Guid.NewGuid().ToString("N");
        var draft = await workflowService.CreateDraftAsync("Scheduled Flow", "", [AutomationTestFactory.Step(startId, WorkflowStepType.Start, endId), AutomationTestFactory.Step(endId, WorkflowStepType.End)], [], "user-1", "org-1", "branch-1");
        var published = await workflowService.PublishAsync(draft.Id);
        return (workflows, published.Id);
    }

    [Fact]
    public async Task CreateAsync_ComputesAnInitialNextRunTimeInTheFuture()
    {
        var (workflows, workflowId) = await SeedPublishedWorkflowAsync();
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, new FakeWorkflowExecutionRepository());
        var service = new ScheduledJobService(new FakeScheduledJobRepository(), engine);

        var job = await service.CreateAsync("Nightly Sync", ScheduleFrequency.Daily, null, workflowId, "org-1", "branch-1");

        Assert.True(job.NextRunAt > DateTimeOffset.UtcNow);
        Assert.True(job.IsEnabled);
    }

    [Fact]
    public async Task GetDueJobsAsync_OnlyReturnsJobsWhoseNextRunHasPassed()
    {
        var (workflows, workflowId) = await SeedPublishedWorkflowAsync();
        var jobs = new FakeScheduledJobRepository();
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, new FakeWorkflowExecutionRepository());
        var service = new ScheduledJobService(jobs, engine);
        var due = await service.CreateAsync("Due Now", ScheduleFrequency.Hourly, null, workflowId, "org-1", "branch-1");
        await jobs.SaveAsync(await ToDomainWithPastNextRun(jobs, due.Id));
        var notDue = await service.CreateAsync("Not Due", ScheduleFrequency.Monthly, null, workflowId, "org-1", "branch-1");

        var dueJobs = await service.GetDueJobsAsync();

        Assert.Single(dueJobs);
        Assert.Equal(due.Id, dueJobs[0].Id);
        Assert.DoesNotContain(dueJobs, job => job.Id == notDue.Id);
    }

    private static async Task<Rojan.Desktop.Domain.Automation.ScheduledJob> ToDomainWithPastNextRun(FakeScheduledJobRepository jobs, string id)
    {
        var existing = await jobs.GetByIdAsync(id) ?? throw new InvalidOperationException("Job not found.");
        return existing with { NextRunAt = DateTimeOffset.UtcNow.AddMinutes(-5) };
    }

    [Fact]
    public async Task RunDueJobAsync_ExecutesTheWorkflowAndAdvancesNextRun()
    {
        var (workflows, workflowId) = await SeedPublishedWorkflowAsync();
        var jobs = new FakeScheduledJobRepository();
        var executions = new FakeWorkflowExecutionRepository();
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, executions);
        var service = new ScheduledJobService(jobs, engine);
        var job = await service.CreateAsync("Nightly Sync", ScheduleFrequency.Daily, null, workflowId, "org-1", "branch-1");
        var originalNextRun = job.NextRunAt;

        var execution = await service.RunDueJobAsync(job.Id);

        Assert.Equal(WorkflowExecutionStatus.Completed, execution.Status);
        var updated = await service.GetByIdAsync(job.Id);
        Assert.NotNull(updated!.LastRunAt);
        Assert.True(updated.NextRunAt > originalNextRun);
    }

    [Fact]
    public async Task SetEnabledAsync_DisablesTheJob()
    {
        var (workflows, workflowId) = await SeedPublishedWorkflowAsync();
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, new FakeWorkflowExecutionRepository());
        var service = new ScheduledJobService(new FakeScheduledJobRepository(), engine);
        var job = await service.CreateAsync("Nightly Sync", ScheduleFrequency.Daily, null, workflowId, "org-1", "branch-1");

        await service.SetEnabledAsync(job.Id, false);

        var updated = await service.GetByIdAsync(job.Id);
        Assert.False(updated!.IsEnabled);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheJob()
    {
        var (workflows, workflowId) = await SeedPublishedWorkflowAsync();
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, new FakeWorkflowExecutionRepository());
        var service = new ScheduledJobService(new FakeScheduledJobRepository(), engine);
        var job = await service.CreateAsync("Nightly Sync", ScheduleFrequency.Daily, null, workflowId, "org-1", "branch-1");

        await service.DeleteAsync(job.Id);

        Assert.Null(await service.GetByIdAsync(job.Id));
    }
}
