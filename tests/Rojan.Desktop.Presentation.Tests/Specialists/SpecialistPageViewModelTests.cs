using Rojan.Desktop.Application.Services;
using Rojan.Desktop.Application.Specialists;
using Rojan.Desktop.Presentation.Tests.Services;
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
            MakeSpecialist(specialistId, "Placeholder"), [], [])));

    /// <summary>Specialist-Service Assignment: an empty catalog by default - only Profile's own construction needs this dependency, not any of this page's own query behavior.</summary>
    private static StubServiceQueryService MakeServiceQueryService() =>
        new(_ => Task.FromResult<IReadOnlyList<ServiceDto>>([]));

    /// <summary>Phase 7.2.6 Shift Engine UI Activation: only Profile's own construction needs these dependencies, not any of this page's own query behavior.</summary>
    private static EmptySpecialistScheduleQueryService MakeScheduleQueryService() => new();

    private static NoOpSpecialistScheduleCommandService MakeScheduleCommandService() => new();

    [Fact]
    public void Constructor_QueryServiceStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<SpecialistDto>>();
        var queryService = new StubSpecialistQueryService(_ => tcs.Task);

        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsSpecialists_StateIsLoadedAndPopulatesSpecialists()
    {
        var specialists = new List<SpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var queryService = new StubSpecialistQueryService(_ => Task.FromResult<IReadOnlyList<SpecialistDto>>(specialists));

        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(specialists, sut.Specialists);
        Assert.Equal(specialists[0], sut.SelectedSpecialist);
        Assert.NotNull(sut.Profile);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsEmptyList_StateIsEmpty()
    {
        var queryService = new StubSpecialistQueryService(_ => Task.FromResult<IReadOnlyList<SpecialistDto>>([]));

        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

        Assert.Equal(DashboardState.Empty, sut.State);
        Assert.Null(sut.SelectedSpecialist);
        Assert.Null(sut.Profile);
    }

    [Fact]
    public void Constructor_QueryServiceThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubSpecialistQueryService(
            _ => Task.FromException<IReadOnlyList<SpecialistDto>>(new InvalidOperationException("boom")));

        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    // Sprint 5 Commit 4: premium specialist search and profile foundation. Text/field-matching
    // behavior now lives in SpecialistQueryService (see SpecialistQueryServiceTests), so these
    // ViewModel tests assert on filter composition (what got asked for) - same split
    // Customers/Bookings/Services' own search commits established.

    [Fact]
    public void Constructor_NoFilterApplied_SearchesWithAnAllDefaultFilter()
    {
        // "Keep existing specialist list behavior unchanged when no filter is applied" - an
        // all-default SpecialistSearchFilter is documented to behave identically to the old
        // unfiltered GetSpecialistsAsync call (see SpecialistSearchFilter's own doc comment).
        var specialists = new List<SpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var queryService = new StubSpecialistQueryService(_ => Task.FromResult<IReadOnlyList<SpecialistDto>>(specialists));

        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

        var filter = Assert.Single(queryService.SearchCalls);
        Assert.Null(filter.SearchText);
        Assert.Null(filter.Status);
        Assert.Null(filter.Skill);
        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(specialists, sut.Specialists);
    }

    [Fact]
    public void SearchText_Changed_SearchesWithSearchTextInFilter()
    {
        var queryService = new StubSpecialistQueryService(_ => Task.FromResult<IReadOnlyList<SpecialistDto>>([]));
        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

        sut.SearchText = "priya";

        Assert.Equal("priya", queryService.SearchCalls[^1].SearchText);
    }

    [Fact]
    public void StatusFilter_Changed_SearchesWithStatusInFilter()
    {
        var queryService = new StubSpecialistQueryService(_ => Task.FromResult<IReadOnlyList<SpecialistDto>>([]));
        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

        sut.StatusFilter = SpecialistStatus.OnLeave;

        Assert.Equal(SpecialistStatus.OnLeave, queryService.SearchCalls[^1].Status);
    }

    [Fact]
    public void SelectedSkill_Changed_SearchesWithSkillInFilter()
    {
        var queryService = new StubSpecialistQueryService(_ => Task.FromResult<IReadOnlyList<SpecialistDto>>([]));
        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

        sut.SelectedSkill = "Balayage";

        Assert.Equal("Balayage", queryService.SearchCalls[^1].Skill);
    }

    [Fact]
    public void SearchText_NoLongerMatchesCurrentSelection_ReselectsFirstFilteredSpecialist()
    {
        var specialists = new List<SpecialistDto>
        {
            MakeSpecialist("specialist-1", "Jordan Lee"),
            MakeSpecialist("specialist-2", "Priya Nair"),
        };
        var queryService = new StubSpecialistQueryService(
            _ => Task.FromResult<IReadOnlyList<SpecialistDto>>(specialists),
            searchSpecialistsByFilter: (filter, _) => Task.FromResult<IReadOnlyList<SpecialistDto>>(
                string.IsNullOrEmpty(filter.SearchText)
                    ? specialists
                    : specialists.Where(specialist => specialist.FullName.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase)).ToList()));
        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());
        sut.SelectedSpecialist = specialists[0];

        sut.SearchText = "Priya";

        Assert.Equal(specialists[1], sut.SelectedSpecialist);
    }

    [Fact]
    public void SearchCommand_Executed_ReRunsSearchWithCurrentFilter()
    {
        var queryService = new StubSpecialistQueryService(_ => Task.FromResult<IReadOnlyList<SpecialistDto>>([]));
        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService())
        {
            SearchText = "priya",
        };
        var callsBeforeExplicitSearch = queryService.SearchCalls.Count;

        sut.SearchCommand.Execute(null);

        Assert.True(queryService.SearchCalls.Count > callsBeforeExplicitSearch);
        Assert.Equal("priya", queryService.SearchCalls[^1].SearchText);
    }

    [Fact]
    public void ClearFiltersCommand_Executed_ResetsEveryFilterAndReloadsWithDefaultFilter()
    {
        var queryService = new StubSpecialistQueryService(_ => Task.FromResult<IReadOnlyList<SpecialistDto>>([]));
        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService())
        {
            SearchText = "priya",
            StatusFilter = SpecialistStatus.OnLeave,
            SelectedSkill = "Balayage",
        };

        sut.ClearFiltersCommand.Execute(null);

        Assert.Equal(string.Empty, sut.SearchText);
        Assert.Null(sut.StatusFilter);
        Assert.Equal(string.Empty, sut.SelectedSkill);

        var filter = queryService.SearchCalls[^1];
        Assert.Null(filter.SearchText);
        Assert.Null(filter.Status);
        Assert.Null(filter.Skill);
    }

    [Fact]
    public void LoadCommand_ExecutedAfterFailure_RecoversToLoadedState()
    {
        var shouldFail = true;
        var specialists = new List<SpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var queryService = new StubSpecialistQueryService(_ => shouldFail
            ? Task.FromException<IReadOnlyList<SpecialistDto>>(new InvalidOperationException("boom"))
            : Task.FromResult<IReadOnlyList<SpecialistDto>>(specialists));
        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());
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
        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

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
        var sut = new SpecialistPageViewModel(queryService, MakeProfileQueryService(), commandService, new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService())
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

    [Fact]
    public void ProfileReportsSuccessfulSave_ReloadsDirectoryAndKeepsSameSpecialistSelected()
    {
        // Specialist Deactivation Wiring: SpecialistProfileViewModel.SpecialistUpdated must reload this
        // page's own directory (so a just-deactivated specialist's status is never left stale in the
        // list) and re-select the same specialist by id afterward - SpecialistDto is a record, so the
        // pre-reload selection (still Active) would otherwise no longer equal its own freshly-reloaded
        // entry and ReplaceAll's "selection no longer present" fallback would jump to the first specialist.
        var specialists = new List<SpecialistDto>
        {
            MakeSpecialist("specialist-1", "Jordan Lee"),
            MakeSpecialist("specialist-2", "Priya Nair"),
        };
        var queryService = new StubSpecialistQueryService(_ => Task.FromResult<IReadOnlyList<SpecialistDto>>(specialists.ToList()));
        var profileQueryService = new StubSpecialistProfileQueryService((specialistId, _) =>
            Task.FromResult(new SpecialistProfileDto(specialists.Single(specialist => specialist.Id == specialistId), [], [])));
        var sut = new SpecialistPageViewModel(queryService, profileQueryService, new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());
        Assert.Equal("specialist-1", sut.SelectedSpecialist?.Id); // sanity: constructor selected the first specialist

        // Simulate the backend having accepted the deactivation by the time the post-save reload runs.
        specialists[0] = specialists[0] with { Status = SpecialistStatus.Inactive };
        sut.Profile!.EditableStatus = SpecialistStatus.Inactive;

        sut.Profile.SaveChangesCommand.Execute(null);

        Assert.Equal("specialist-1", sut.SelectedSpecialist?.Id);
        Assert.Equal(SpecialistStatus.Inactive, sut.SelectedSpecialist?.Status);
        Assert.Contains(sut.Specialists, specialist => specialist.Id == "specialist-1" && specialist.Status == SpecialistStatus.Inactive);
    }
}
