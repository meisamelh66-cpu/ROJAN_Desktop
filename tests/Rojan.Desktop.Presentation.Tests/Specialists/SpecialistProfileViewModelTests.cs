using Rojan.Desktop.Application.Intelligence;
using Rojan.Desktop.Application.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Specialists;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

public sealed class SpecialistProfileViewModelTests
{
    private static SpecialistProfileDto MakeProfile(string specialistId = "specialist-1") =>
        new(
            new SpecialistDto(specialistId, "Jordan Lee", "Senior Colour Specialist", "jordan.lee@rojan.example", "555-0100", SpecialistStatus.Active, "Specializes in balayage."),
            [new SpecialistSkillDto("skill-1", specialistId, "Colour")]);

    private static SpecialistIntelligenceDto MakeIntelligence(string specialistId = "specialist-1") =>
        new(specialistId, "Jordan Lee", 55, SpecialistPerformanceLevel.Good, SpecialistRecommendationSignal.Maintain, 6, 1, 0);

    [Fact]
    public void Constructor_ProfileQueryStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<SpecialistProfileDto>();
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => tcs.Task);
        var commandService = new StubSpecialistCommandService();

        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine());

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_ProfileQueryReturnsProfile_PopulatesSpecialistAndSkills()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService();

        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine());

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal("Jordan Lee", sut.Specialist?.FullName);
        Assert.Equal(SpecialistStatus.Active, sut.EditableStatus);
        Assert.Single(sut.Skills);
    }

    [Fact]
    public void Constructor_ProfileQueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromException<SpecialistProfileDto>(new InvalidOperationException("boom")));
        var commandService = new StubSpecialistCommandService();

        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void AddSkillCommand_TextIsEmpty_CanExecuteIsFalse()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService();
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine());

        Assert.False(sut.AddSkillCommand.CanExecute(null));

        sut.NewSkillText = "Massage";

        Assert.True(sut.AddSkillCommand.CanExecute(null));
    }

    [Fact]
    public void AddSkillCommand_Executed_CallsCommandServiceWithSpecialistIdAndNameThenClearsInput()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService();
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine())
        {
            NewSkillText = "Massage",
        };

        sut.AddSkillCommand.Execute(null);

        var call = Assert.Single(commandService.AddSkillCalls);
        Assert.Equal("specialist-1", call.SpecialistId);
        Assert.Equal("Massage", call.Name);
        Assert.Equal(string.Empty, sut.NewSkillText);
    }

    [Fact]
    public void RemoveSkillCommand_Executed_CallsCommandServiceWithSpecialistIdAndSkillId()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService();
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine());
        var skill = new SpecialistSkillDto("skill-1", "specialist-1", "Colour");

        sut.RemoveSkillCommand.Execute(skill);

        var call = Assert.Single(commandService.RemoveSkillCalls);
        Assert.Equal("specialist-1", call.SpecialistId);
        Assert.Equal("skill-1", call.SkillId);
    }

    [Fact]
    public void SaveChangesCommand_Executed_CallsUpdateSpecialistAsyncWithEditableStatus()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService();
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine())
        {
            EditableStatus = SpecialistStatus.OnLeave,
        };

        sut.SaveChangesCommand.Execute(null);

        var request = Assert.Single(commandService.UpdateRequests);
        Assert.Equal("specialist-1", request.Id);
        Assert.Equal(SpecialistStatus.OnLeave, request.Status);
    }

    // Sprint 5 Commit 5C: Intelligence integration. IntelligenceEngineTests (Application.Tests)
    // already covers every score/level/signal calculation - these tests only assert the
    // ViewModel requests IIntelligenceEngine, picks out the matching entry, and exposes it
    // as-is; no calculation is duplicated here.

    [Fact]
    public void Constructor_IntelligenceEngineReturnsMatchingEntry_PopulatesIntelligenceProperties()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var intelligenceEngine = new StubIntelligenceEngine([MakeIntelligence()]);

        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, new StubSpecialistCommandService(), intelligenceEngine);

        Assert.True(sut.HasIntelligence);
        Assert.Equal(55, sut.PerformanceScore);
        Assert.Equal(SpecialistPerformanceLevel.Good, sut.PerformanceLevel);
        Assert.Equal(SpecialistRecommendationSignal.Maintain, sut.RecommendationSignal);
        Assert.Equal(6, sut.CompletedBookingCount);
        Assert.Equal(1, sut.CancelledBookingCount);
        Assert.Equal(0, sut.NoShowBookingCount);
    }

    [Fact]
    public void Constructor_IntelligenceEngineReturnsNoMatchingEntry_HasIntelligenceIsFalseAndValuesDefaultToZero()
    {
        // Empty Intelligence state / null safety: the engine has no entry for this specialist
        // (e.g. a brand new one with no bookings yet, or a transient mismatch) - the ViewModel
        // must not throw, and must expose safe defaults rather than propagating a null DTO.
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var intelligenceEngine = new StubIntelligenceEngine([MakeIntelligence(specialistId: "someone-else")]);

        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, new StubSpecialistCommandService(), intelligenceEngine);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.False(sut.HasIntelligence);
        Assert.Equal(0, sut.PerformanceScore);
        Assert.Equal(SpecialistPerformanceLevel.Underperforming, sut.PerformanceLevel);
        Assert.Equal(SpecialistRecommendationSignal.Attention, sut.RecommendationSignal);
        Assert.Equal(0, sut.CompletedBookingCount);
        Assert.Equal(0, sut.CancelledBookingCount);
        Assert.Equal(0, sut.NoShowBookingCount);
    }

    [Fact]
    public void Constructor_IntelligenceEngineReturnsEmptyList_HasIntelligenceIsFalse()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var intelligenceEngine = new StubIntelligenceEngine([]);

        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, new StubSpecialistCommandService(), intelligenceEngine);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.False(sut.HasIntelligence);
    }

    [Fact]
    public void LoadCommand_ExecutedAfterIntelligenceChanges_RefreshesIntelligenceProperties()
    {
        // Refresh behavior: a reload must pick up the engine's latest data, not cache the first result.
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var intelligenceEngine = new StubIntelligenceEngine([MakeIntelligence()]);
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, new StubSpecialistCommandService(), intelligenceEngine);
        Assert.Equal(55, sut.PerformanceScore);

        intelligenceEngine.SpecialistIntelligence =
        [
            new SpecialistIntelligenceDto("specialist-1", "Jordan Lee", 80, SpecialistPerformanceLevel.Excellent, SpecialistRecommendationSignal.Promote, 8, 0, 0),
        ];
        sut.LoadCommand.Execute(null);

        Assert.Equal(80, sut.PerformanceScore);
        Assert.Equal(SpecialistPerformanceLevel.Excellent, sut.PerformanceLevel);
        Assert.Equal(SpecialistRecommendationSignal.Promote, sut.RecommendationSignal);
    }

    [Fact]
    public void Intelligence_PropertiesRaisePropertyChangedWhenLoaded()
    {
        var intelligenceEngine = new StubIntelligenceEngine();
        var tcs = new TaskCompletionSource<SpecialistProfileDto>();
        var slowProfileQuery = new StubSpecialistProfileQueryService((_, _) => tcs.Task);
        var sut = new SpecialistProfileViewModel("specialist-1", slowProfileQuery, new StubSpecialistCommandService(), intelligenceEngine);
        var raisedProperties = new List<string>();
        sut.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is not null)
            {
                raisedProperties.Add(args.PropertyName);
            }
        };

        intelligenceEngine.SpecialistIntelligence = [MakeIntelligence()];
        tcs.SetResult(MakeProfile());

        Assert.Contains(nameof(SpecialistProfileViewModel.HasIntelligence), raisedProperties);
        Assert.Contains(nameof(SpecialistProfileViewModel.PerformanceScore), raisedProperties);
        Assert.Contains(nameof(SpecialistProfileViewModel.PerformanceLevel), raisedProperties);
        Assert.Contains(nameof(SpecialistProfileViewModel.RecommendationSignal), raisedProperties);
    }
}
