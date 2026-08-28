using Microsoft.Extensions.Logging;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Automation;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Automation;

public sealed class WorkflowsTabViewModelTests
{
    private const string Secret = "workflow-definition-SECRET-vip";

    private static (WorkflowsTabViewModel Sut, StubWorkflowService Workflows, StubWorkflowExecutionEngine Engine) CreateSut()
    {
        var workflows = new StubWorkflowService();
        var engine = new StubWorkflowExecutionEngine();
        var sut = new WorkflowsTabViewModel(workflows, engine, "user-1", "org-1", "branch-1");
        return (sut, workflows, engine);
    }

    private static (WorkflowsTabViewModel Sut, StubWorkflowService Workflows, StubWorkflowExecutionEngine Engine, RecordingLogger<WorkflowsTabViewModel> Logger) CreateLoggedSut()
    {
        var workflows = new StubWorkflowService();
        var engine = new StubWorkflowExecutionEngine();
        var logger = new RecordingLogger<WorkflowsTabViewModel>();
        var sut = new WorkflowsTabViewModel(workflows, engine, "user-1", "org-1", "branch-1", logger);
        return (sut, workflows, engine, logger);
    }

    private static void AssertSingleErrorFor(RecordingLogger<WorkflowsTabViewModel> logger, string operation)
    {
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=" + operation, entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak()
    {
        var (sut, workflows, _, logger) = CreateLoggedSut();
        workflows.GetAllException = new InvalidOperationException(Secret);

        await sut.LoadAsync();

        Assert.Equal(DashboardState.Error, sut.State);
        AssertSingleErrorFor(logger, nameof(sut.LoadAsync));
    }

    [Fact]
    public void CreateDraftAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak()
    {
        var (sut, workflows, _, logger) = CreateLoggedSut();
        workflows.CreateDraftException = new InvalidOperationException(Secret);
        sut.NewWorkflowName = "Welcome Flow";

        sut.CreateDraftCommand.Execute(null);

        AssertSingleErrorFor(logger, "CreateDraftAsync");
    }

    [Fact]
    public void PublishAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak()
    {
        var (sut, workflows, _, logger) = CreateLoggedSut();
        sut.NewWorkflowName = "Welcome Flow";
        sut.CreateDraftCommand.Execute(null);
        workflows.PublishException = new InvalidOperationException(Secret);

        sut.PublishCommand.Execute(sut.Workflows[0]);

        AssertSingleErrorFor(logger, "PublishAsync");
    }

    [Fact]
    public void RunNowAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak()
    {
        var (sut, _, engine, logger) = CreateLoggedSut();
        sut.NewWorkflowName = "Welcome Flow";
        sut.CreateDraftCommand.Execute(null);
        engine.ExecuteException = new InvalidOperationException(Secret);

        sut.RunNowCommand.Execute(sut.Workflows[0]);

        AssertSingleErrorFor(logger, "RunNowAsync");
    }

    [Fact]
    public void RollbackAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak()
    {
        var (sut, workflows, _, logger) = CreateLoggedSut();
        sut.NewWorkflowName = "Welcome Flow";
        sut.CreateDraftCommand.Execute(null);
        workflows.RollbackException = new InvalidOperationException(Secret);

        sut.RollbackCommand.Execute(sut.Workflows[0]);

        AssertSingleErrorFor(logger, "RollbackAsync");
    }

    [Fact]
    public async Task LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows()
    {
        var (sut, workflows, _) = CreateSut();
        workflows.GetAllException = new InvalidOperationException("boom");

        await sut.LoadAsync();

        Assert.Equal(DashboardState.Error, sut.State);
    }

    [Fact]
    public void LoadCommand_NoWorkflowsYet_StateIsEmpty()
    {
        var (sut, _, _) = CreateSut();

        sut.LoadCommand.Execute(null);

        Assert.Equal(DashboardState.Empty, sut.State);
        Assert.Empty(sut.Workflows);
    }

    [Fact]
    public void CreateDraftCommand_CanExecute_OnlyWhenNameIsProvided()
    {
        var (sut, _, _) = CreateSut();

        Assert.False(sut.CreateDraftCommand.CanExecute(null));

        sut.NewWorkflowName = "Welcome Flow";

        Assert.True(sut.CreateDraftCommand.CanExecute(null));
    }

    [Fact]
    public void CreateDraftCommand_AddsANewDraftWithAMinimalStartEndSkeleton()
    {
        var (sut, _, _) = CreateSut();
        sut.NewWorkflowName = "Welcome Flow";

        sut.CreateDraftCommand.Execute(null);

        Assert.Single(sut.Workflows);
        Assert.Equal("Welcome Flow", sut.Workflows[0].Name);
        Assert.Equal(2, sut.Workflows[0].Steps.Count);
        Assert.Equal(string.Empty, sut.NewWorkflowName);
    }

    [Fact]
    public void PublishCommand_MarksTheWorkflowPublished()
    {
        var (sut, _, _) = CreateSut();
        sut.NewWorkflowName = "Welcome Flow";
        sut.CreateDraftCommand.Execute(null);
        var draft = sut.Workflows[0];

        sut.PublishCommand.Execute(draft);

        Assert.Equal(Rojan.Desktop.Application.Automation.WorkflowStatus.Published, sut.Workflows[0].Status);
    }

    [Fact]
    public void RunNowCommand_InvokesTheExecutionEngine()
    {
        var (sut, _, engine) = CreateSut();
        sut.NewWorkflowName = "Welcome Flow";
        sut.CreateDraftCommand.Execute(null);
        var draft = sut.Workflows[0];

        sut.RunNowCommand.Execute(draft);

        Assert.Equal(1, engine.ExecuteCallCount);
    }

    [Fact]
    public void DeleteCommand_RemovesTheWorkflow()
    {
        var (sut, _, _) = CreateSut();
        sut.NewWorkflowName = "Welcome Flow";
        sut.CreateDraftCommand.Execute(null);
        var draft = sut.Workflows[0];

        sut.DeleteCommand.Execute(draft);

        Assert.Empty(sut.Workflows);
    }

    [Fact]
    public void SelectingAWorkflow_LoadsItsVersionHistory()
    {
        var (sut, _, _) = CreateSut();
        sut.NewWorkflowName = "Welcome Flow";
        sut.CreateDraftCommand.Execute(null);
        var draft = sut.Workflows[0];
        sut.PublishCommand.Execute(draft);
        var published = sut.Workflows[0];

        sut.RollbackCommand.Execute(published);
        sut.SelectedWorkflow = sut.Workflows.First(w => w.Status == Rojan.Desktop.Application.Automation.WorkflowStatus.Draft);

        Assert.NotEmpty(sut.VersionHistory);
    }
}
