using Rojan.Desktop.Application.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Specialists;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

public sealed class SpecialistPageViewModelTests
{
    private static SpecialistDto MakeSpecialist(string id, string fullName, string title = "", string email = "") =>
        new(id, fullName, title, email, string.Empty, SpecialistStatus.Active, string.Empty);

    /// <summary>A profile query stub that never fails, used by tests that don't assert on Profile - Profile is constructed as a side effect of selection, and its own errors are contained internally.</summary>
    private static StubSpecialistProfileQueryService MakeProfileQueryService() =>
        new((specialistId, _) => Task.FromResult(new SpecialistProfileDto(
            MakeSpecialist(specialistId, "Placeholder"), [])));

    [Fact]
    public void Constructor_QueryServiceStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<SpecialistDto>>();
        var queryService = new StubSpecialistQueryService(_ => tcs.Task);

        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService());

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsSpecialists_StateIsLoadedAndPopulatesSpecialists()
    {
        var specialists = new List<SpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var queryService = new StubSpecialistQueryService(_ => Task.FromResult<IReadOnlyList<SpecialistDto>>(specialists));

        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService());

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(specialists, sut.Specialists);
        Assert.Equal(specialists[0], sut.SelectedSpecialist);
        Assert.NotNull(sut.Profile);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsEmptyList_StateIsEmpty()
    {
        var queryService = new StubSpecialistQueryService(_ => Task.FromResult<IReadOnlyList<SpecialistDto>>([]));

        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService());

        Assert.Equal(DashboardState.Empty, sut.State);
        Assert.Null(sut.SelectedSpecialist);
        Assert.Null(sut.Profile);
    }

    [Fact]
    public void Constructor_QueryServiceThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubSpecialistQueryService(
            _ => Task.FromException<IReadOnlyList<SpecialistDto>>(new InvalidOperationException("boom")));

        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void SearchText_MatchesNameTitleOrEmail_FiltersToMatchingSpecialistsOnly()
    {
        var specialists = new List<SpecialistDto>
        {
            MakeSpecialist("specialist-1", "Jordan Lee", title: "Senior Colour Specialist"),
            MakeSpecialist("specialist-2", "Priya Nair", email: "priya.nair@rojan.example"),
            MakeSpecialist("specialist-3", "Casey Morgan", title: "Spa Therapist"),
        };
        var queryService = new StubSpecialistQueryService(_ => Task.FromResult<IReadOnlyList<SpecialistDto>>(specialists));
        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService());

        sut.SearchText = "therapist";

        Assert.Equal(["specialist-3"], sut.Specialists.Select(s => s.Id));
    }

    [Fact]
    public void SearchText_NoLongerMatchesCurrentSelection_ReselectsFirstFilteredSpecialist()
    {
        var specialists = new List<SpecialistDto>
        {
            MakeSpecialist("specialist-1", "Jordan Lee"),
            MakeSpecialist("specialist-2", "Priya Nair"),
        };
        var queryService = new StubSpecialistQueryService(_ => Task.FromResult<IReadOnlyList<SpecialistDto>>(specialists));
        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService());
        sut.SelectedSpecialist = specialists[0];

        sut.SearchText = "Priya";

        Assert.Equal(specialists[1], sut.SelectedSpecialist);
    }

    [Fact]
    public void LoadCommand_ExecutedAfterFailure_RecoversToLoadedState()
    {
        var shouldFail = true;
        var specialists = new List<SpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var queryService = new StubSpecialistQueryService(_ => shouldFail
            ? Task.FromException<IReadOnlyList<SpecialistDto>>(new InvalidOperationException("boom"))
            : Task.FromResult<IReadOnlyList<SpecialistDto>>(specialists));
        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService());
        Assert.Equal(DashboardState.Error, sut.State);

        shouldFail = false;
        sut.LoadCommand.Execute(null);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(specialists, sut.Specialists);
    }

    [Fact]
    public void CreateSpecialistCommand_FullNameIsEmpty_CanExecuteIsFalse()
    {
        var queryService = new StubSpecialistQueryService(_ => Task.FromResult<IReadOnlyList<SpecialistDto>>([]));
        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService());

        Assert.False(sut.CreateSpecialistCommand.CanExecute(null));

        sut.NewSpecialistFullName = "Riley Chen";

        Assert.True(sut.CreateSpecialistCommand.CanExecute(null));
    }

    [Fact]
    public void CreateSpecialistCommand_Executed_CallsCommandServiceReloadsListAndSelectsNewSpecialist()
    {
        var existing = new List<SpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var queryService = new StubSpecialistQueryService(_ => Task.FromResult<IReadOnlyList<SpecialistDto>>(existing.ToList()));
        var commandService = new StubSpecialistCommandService
        {
            OnSpecialistCreated = (_, dto) => existing.Add(dto),
        };
        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), commandService)
        {
            NewSpecialistFullName = "Riley Chen",
            NewSpecialistTitle = "Junior Stylist",
            NewSpecialistEmail = "riley.chen@rojan.example",
            NewSpecialistPhone = "555-0199",
        };

        sut.CreateSpecialistCommand.Execute(null);

        var request = Assert.Single(commandService.CreateRequests);
        Assert.Equal("Riley Chen", request.FullName);
        Assert.Equal(string.Empty, sut.NewSpecialistFullName);
        Assert.Equal("new-specialist", sut.SelectedSpecialist?.Id);
    }
}
