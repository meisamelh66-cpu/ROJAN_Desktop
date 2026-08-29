using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Automation;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Automation;

public sealed class ScheduledJobsTabViewModelTests
{
    private const string Secret = "cron-0-9-star-star-1-SECRET";

    private static WorkflowDefinitionDto PublishedWorkflow() => new(
        "w1", "w1", "Flow", "", [], [], WorkflowStatus.Published, 1, true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "user-1", "org-1", "branch-1");

    private static (ScheduledJobsTabViewModel Sut, StubScheduledJobService Jobs) CreateSut(StubWorkflowService? workflows = null)
    {
        var jobs = new StubScheduledJobService();
        var sut = new ScheduledJobsTabViewModel(jobs, workflows ?? new StubWorkflowService(), "org-1", "branch-1");
        return (sut, jobs);
    }

    [Fact]
    public async Task LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak()
    {
        var jobs = new StubScheduledJobService { GetAllException = new InvalidOperationException(Secret) };
        var logger = new RecordingLogger<ScheduledJobsTabViewModel>();
        var sut = new ScheduledJobsTabViewModel(jobs, new StubWorkflowService(), "org-1", "branch-1", logger);

        await sut.LoadAsync();

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.DoesNotContain(Secret, sut.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=LoadAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak()
    {
        var jobs = new StubScheduledJobService { CreateException = new InvalidOperationException(Secret) };
        var logger = new RecordingLogger<ScheduledJobsTabViewModel>();
        var sut = new ScheduledJobsTabViewModel(jobs, new StubWorkflowService(), "org-1", "branch-1", logger);
        sut.NewJobName = "Nightly Sync";
        sut.NewJobWorkflow = PublishedWorkflow();

        sut.CreateCommand.Execute(null);

        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.DoesNotContain(Secret, sut.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=CreateAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RunNowAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak()
    {
        var jobs = new StubScheduledJobService();
        var logger = new RecordingLogger<ScheduledJobsTabViewModel>();
        var sut = new ScheduledJobsTabViewModel(jobs, new StubWorkflowService(), "org-1", "branch-1", logger);
        sut.NewJobName = "Nightly Sync";
        sut.NewJobWorkflow = PublishedWorkflow();
        sut.CreateCommand.Execute(null);
        jobs.RunDueJobException = new InvalidOperationException(Secret);

        sut.RunNowCommand.Execute(sut.Jobs[0]);

        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.DoesNotContain(Secret, sut.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=RunNowAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows()
    {
        var jobs = new StubScheduledJobService { GetAllException = new InvalidOperationException("boom") };
        var sut = new ScheduledJobsTabViewModel(jobs, new StubWorkflowService(), "org-1", "branch-1");

        await sut.LoadAsync();

        Assert.Equal(DashboardState.Error, sut.State);
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
    public void ToggleEnabledCommand_Failure_ShowsGenericError_PreservesJobState_LogsOperationOnly()
    {
        var jobs = new StubScheduledJobService();
        var logger = new RecordingLogger<ScheduledJobsTabViewModel>();
        var sut = new ScheduledJobsTabViewModel(jobs, new StubWorkflowService(), "org-1", "branch-1", logger);
        sut.NewJobName = "Nightly Sync";
        sut.NewJobWorkflow = PublishedWorkflow();
        sut.CreateCommand.Execute(null);
        jobs.SetEnabledException = new InvalidOperationException(Secret);

        var exception = Record.Exception(() => sut.ToggleEnabledCommand.Execute(sut.Jobs[0]));

        Assert.Null(exception);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.True(sut.Jobs[0].IsEnabled);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=ToggleEnabledAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ToggleEnabledCommand_SuccessAfterFailure_ClearsError()
    {
        var jobs = new StubScheduledJobService();
        var sut = new ScheduledJobsTabViewModel(jobs, new StubWorkflowService(), "org-1", "branch-1");
        sut.NewJobName = "Nightly Sync";
        sut.NewJobWorkflow = PublishedWorkflow();
        sut.CreateCommand.Execute(null);
        jobs.SetEnabledException = new InvalidOperationException(Secret);
        sut.ToggleEnabledCommand.Execute(sut.Jobs[0]);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);

        jobs.SetEnabledException = null;
        sut.ToggleEnabledCommand.Execute(sut.Jobs[0]);

        Assert.Null(sut.ErrorMessage);
        Assert.False(sut.Jobs[0].IsEnabled);
    }

    [Fact]
    public void DeleteCommand_Failure_ShowsGenericError_PreservesJob_LogsOperationOnly()
    {
        var jobs = new StubScheduledJobService();
        var logger = new RecordingLogger<ScheduledJobsTabViewModel>();
        var sut = new ScheduledJobsTabViewModel(jobs, new StubWorkflowService(), "org-1", "branch-1", logger);
        sut.NewJobName = "Nightly Sync";
        sut.NewJobWorkflow = PublishedWorkflow();
        sut.CreateCommand.Execute(null);
        jobs.DeleteException = new InvalidOperationException(Secret);

        var exception = Record.Exception(() => sut.DeleteCommand.Execute(sut.Jobs[0]));

        Assert.Null(exception);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.Single(sut.Jobs);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=DeleteAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
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
