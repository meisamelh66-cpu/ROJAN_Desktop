using Rojan.Desktop.Presentation.ViewModels.Automation;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Automation;

public sealed class WorkflowsTabViewModelTests
{
    private static (WorkflowsTabViewModel Sut, StubWorkflowService Workflows, StubWorkflowExecutionEngine Engine) CreateSut()
    {
        var workflows = new StubWorkflowService();
        var engine = new StubWorkflowExecutionEngine();
        var sut = new WorkflowsTabViewModel(workflows, engine, "user-1", "org-1", "branch-1");
        return (sut, workflows, engine);
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
