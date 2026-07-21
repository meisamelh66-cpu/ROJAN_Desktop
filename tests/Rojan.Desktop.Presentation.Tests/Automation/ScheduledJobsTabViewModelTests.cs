using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Presentation.ViewModels.Automation;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Automation;

public sealed class ScheduledJobsTabViewModelTests
{
    private static WorkflowDefinitionDto PublishedWorkflow() => new(
        "w1", "w1", "Flow", "", [], [], WorkflowStatus.Published, 1, true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "user-1", "org-1", "branch-1");

    private static (ScheduledJobsTabViewModel Sut, StubScheduledJobService Jobs) CreateSut(StubWorkflowService? workflows = null)
    {
        var jobs = new StubScheduledJobService();
        var sut = new ScheduledJobsTabViewModel(jobs, workflows ?? new StubWorkflowService(), "org-1", "branch-1");
        return (sut, jobs);
    }

    [Fact]
    public void LoadCommand_NoJobsYet_StateIsEmpty()
    {
        var (sut, _) = CreateSut();

        sut.LoadCommand.Execute(null);

        Assert.Equal(DashboardState.Empty, sut.State);
    }

    [Fact]
    public void CreateCommand_CanExecute_OnlyWhenNameAndWorkflowAreProvided()
    {
        var (sut, _) = CreateSut();

        Assert.False(sut.CreateCommand.CanExecute(null));

        sut.NewJobName = "Nightly Sync";
        Assert.False(sut.CreateCommand.CanExecute(null));

        sut.NewJobWorkflow = PublishedWorkflow();
        Assert.True(sut.CreateCommand.CanExecute(null));
    }

    [Fact]
    public void CreateCommand_AddsANewEnabledJob()
    {
        var (sut, _) = CreateSut();
        sut.NewJobName = "Nightly Sync";
        sut.NewJobFrequency = ScheduleFrequency.Daily;
        sut.NewJobWorkflow = PublishedWorkflow();

        sut.CreateCommand.Execute(null);

        Assert.Single(sut.Jobs);
        Assert.True(sut.Jobs[0].IsEnabled);
        Assert.Equal(ScheduleFrequency.Daily, sut.Jobs[0].Frequency);
        Assert.Equal(string.Empty, sut.NewJobName);
        Assert.Null(sut.NewJobWorkflow);
    }

    [Fact]
    public void ToggleEnabledCommand_FlipsIsEnabled()
    {
        var (sut, _) = CreateSut();
        sut.NewJobName = "Nightly Sync";
        sut.NewJobWorkflow = PublishedWorkflow();
        sut.CreateCommand.Execute(null);
        var job = sut.Jobs[0];

        sut.ToggleEnabledCommand.Execute(job);

        Assert.False(sut.Jobs[0].IsEnabled);
    }

    [Fact]
    public void RunNowCommand_ExecutesTheJobAndReloads()
    {
        var (sut, _) = CreateSut();
        sut.NewJobName = "Nightly Sync";
        sut.NewJobWorkflow = PublishedWorkflow();
        sut.CreateCommand.Execute(null);
        var job = sut.Jobs[0];

        sut.RunNowCommand.Execute(job);

        Assert.Equal(DashboardState.Loaded, sut.State);
    }

    [Fact]
    public void DeleteCommand_RemovesTheJob()
    {
        var (sut, _) = CreateSut();
        sut.NewJobName = "Nightly Sync";
        sut.NewJobWorkflow = PublishedWorkflow();
        sut.CreateCommand.Execute(null);
        var job = sut.Jobs[0];

        sut.DeleteCommand.Execute(job);

        Assert.Empty(sut.Jobs);
    }
}
