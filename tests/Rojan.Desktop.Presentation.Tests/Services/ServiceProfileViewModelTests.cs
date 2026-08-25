using Rojan.Desktop.Application.Intelligence;
using Rojan.Desktop.Application.Services;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Services;

namespace Rojan.Desktop.Presentation.Tests.Services;

public sealed class ServiceProfileViewModelTests
{
    private static ServiceProfileDto MakeProfile(string serviceId = "service-1") =>
        new(
            new ServiceDto(serviceId, "Haircut & Style", ServiceCategory.Hair, ServiceStatus.Active, 60, "$65", "Classic cut and blow-dry finish."),
            [new AssignedSpecialistDto("assignment-1", serviceId, "specialist-1", "Jordan Lee")]);

    private static ServiceIntelligenceDto MakeIntelligence(string serviceId = "service-1") =>
        new(serviceId, "Haircut & Style", 40, ServicePopularityLevel.Trending, ServiceRecommendationSignal.Maintain, 4, 2);

    [Fact]
    public void Constructor_ProfileQueryStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<ServiceProfileDto>();
        var profileQuery = new StubServiceProfileQueryService((_, _) => tcs.Task);
        var commandService = new StubServiceCommandService();

        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService, new StubIntelligenceEngine());

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_ProfileQueryReturnsProfile_PopulatesServiceAndAssignedSpecialists()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubServiceCommandService();

        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService, new StubIntelligenceEngine());

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal("Haircut & Style", sut.Service?.Name);
        Assert.Single(sut.AssignedSpecialists);
    }

    [Fact]
    public void Constructor_ProfileQueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromException<ServiceProfileDto>(new InvalidOperationException("boom")));
        var commandService = new StubServiceCommandService();

        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService, new StubIntelligenceEngine());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void AssignSpecialistCommand_NameIsEmpty_CanExecuteIsFalse()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubServiceCommandService();
        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService, new StubIntelligenceEngine());

        Assert.False(sut.AssignSpecialistCommand.CanExecute(null));

        sut.NewSpecialistName = "Casey Morgan";

        Assert.True(sut.AssignSpecialistCommand.CanExecute(null));
    }

    [Fact]
    public void AssignSpecialistCommand_Executed_CallsCommandServiceWithServiceIdAndNameThenClearsInput()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubServiceCommandService();
        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService, new StubIntelligenceEngine())
        {
            NewSpecialistName = "Casey Morgan",
        };

        sut.AssignSpecialistCommand.Execute(null);

        var call = Assert.Single(commandService.AssignCalls);
        Assert.Equal("service-1", call.ServiceId);
        Assert.Equal("Casey Morgan", call.SpecialistName);
        Assert.Equal(string.Empty, sut.NewSpecialistName);
    }

    [Fact]
    public void UnassignSpecialistCommand_Executed_CallsCommandServiceWithServiceIdAndAssignmentId()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubServiceCommandService();
        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService, new StubIntelligenceEngine());
        var assignment = new AssignedSpecialistDto("assignment-1", "service-1", "specialist-1", "Jordan Lee");

        sut.UnassignSpecialistCommand.Execute(assignment);

        var call = Assert.Single(commandService.UnassignCalls);
        Assert.Equal("service-1", call.ServiceId);
        Assert.Equal("assignment-1", call.AssignmentId);
    }

    [Fact]
    public void Constructor_PopulatesEditableFieldsFromLoadedService()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var sut = new ServiceProfileViewModel("service-1", profileQuery, new StubServiceCommandService(), new StubIntelligenceEngine());

        Assert.Equal("Haircut & Style", sut.EditableName);
        Assert.Equal("Classic cut and blow-dry finish.", sut.EditableDescription);
        Assert.Equal(60, sut.EditableDurationMinutes);
        Assert.Equal(65m, sut.EditablePrice);
    }

    [Fact]
    public void SaveChangesCommand_NameIsEmpty_CanExecuteIsFalse()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var sut = new ServiceProfileViewModel("service-1", profileQuery, new StubServiceCommandService(), new StubIntelligenceEngine())
        {
            EditableName = string.Empty,
        };

        Assert.False(sut.SaveChangesCommand.CanExecute(null));
    }

    [Fact]
    public void SaveChangesCommand_Executed_CallsUpdateServiceAsyncWithEditableFields()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubServiceCommandService();
        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService, new StubIntelligenceEngine())
        {
            EditableName = "Haircut & Style (updated)",
            EditableDurationMinutes = 75,
            EditablePrice = 700000m,
            EditableDescription = "Updated description.",
        };

        sut.SaveChangesCommand.Execute(null);

        var call = Assert.Single(commandService.UpdateCalls);
        Assert.Equal("service-1", call.Id);
        Assert.Equal("Haircut & Style (updated)", call.Name);
        Assert.Equal(75, call.DurationMinutes);
        Assert.Equal(700000m, call.Price);
    }

    [Fact]
    public void DeactivateCommand_ServiceIsActive_CanExecuteIsTrue()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var sut = new ServiceProfileViewModel("service-1", profileQuery, new StubServiceCommandService(), new StubIntelligenceEngine());

        Assert.True(sut.DeactivateCommand.CanExecute(null));
    }

    [Fact]
    public void DeactivateCommand_ServiceIsAlreadyDiscontinued_CanExecuteIsFalse()
    {
        var profile = new ServiceProfileDto(
            new ServiceDto("service-1", "Old Service", ServiceCategory.Hair, ServiceStatus.Discontinued, 60, "$65", ""), []);
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(profile));
        var sut = new ServiceProfileViewModel("service-1", profileQuery, new StubServiceCommandService(), new StubIntelligenceEngine());

        Assert.False(sut.DeactivateCommand.CanExecute(null));
    }

    [Fact]
    public void DeactivateCommand_Executed_CallsDeactivateServiceAsyncWithServiceId()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubServiceCommandService();
        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService, new StubIntelligenceEngine());

        sut.DeactivateCommand.Execute(null);

        var call = Assert.Single(commandService.DeactivateCalls);
        Assert.Equal("service-1", call);
    }

    // Sprint 5 Commit 5C: Intelligence integration. IntelligenceEngineTests (Application.Tests)
    // already covers every score/level/signal calculation - these tests only assert the
    // ViewModel requests IIntelligenceEngine, picks out the matching entry, and exposes it
    // as-is; no calculation is duplicated here.

    [Fact]
    public void Constructor_IntelligenceEngineReturnsMatchingEntry_PopulatesIntelligenceProperties()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var intelligenceEngine = new StubIntelligenceEngine([MakeIntelligence()]);

        var sut = new ServiceProfileViewModel("service-1", profileQuery, new StubServiceCommandService(), intelligenceEngine);

        Assert.True(sut.HasIntelligence);
        Assert.Equal(40, sut.PopularityScore);
        Assert.Equal(ServicePopularityLevel.Trending, sut.PopularityLevel);
        Assert.Equal(ServiceRecommendationSignal.Maintain, sut.RecommendationSignal);
        Assert.Equal(4, sut.CompletedBookingCount);
        Assert.Equal(2, sut.UpcomingBookingCount);
    }

    [Fact]
    public void Constructor_IntelligenceEngineReturnsNoMatchingEntry_HasIntelligenceIsFalseAndValuesDefaultToZero()
    {
        // Empty Intelligence state / null safety: the engine has no entry for this service
        // (e.g. a brand new one with no bookings yet, or a transient mismatch) - the ViewModel
        // must not throw, and must expose safe defaults rather than propagating a null DTO.
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var intelligenceEngine = new StubIntelligenceEngine([MakeIntelligence(serviceId: "someone-else")]);

        var sut = new ServiceProfileViewModel("service-1", profileQuery, new StubServiceCommandService(), intelligenceEngine);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.False(sut.HasIntelligence);
        Assert.Equal(0, sut.PopularityScore);
        Assert.Equal(ServicePopularityLevel.LowDemand, sut.PopularityLevel);
        Assert.Equal(ServiceRecommendationSignal.Reconsider, sut.RecommendationSignal);
        Assert.Equal(0, sut.CompletedBookingCount);
        Assert.Equal(0, sut.UpcomingBookingCount);
    }

    [Fact]
    public void Constructor_IntelligenceEngineReturnsEmptyList_HasIntelligenceIsFalse()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var intelligenceEngine = new StubIntelligenceEngine([]);

        var sut = new ServiceProfileViewModel("service-1", profileQuery, new StubServiceCommandService(), intelligenceEngine);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.False(sut.HasIntelligence);
    }

    [Fact]
    public void LoadCommand_ExecutedAfterIntelligenceChanges_RefreshesIntelligenceProperties()
    {
        // Refresh behavior: a reload must pick up the engine's latest data, not cache the first result.
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var intelligenceEngine = new StubIntelligenceEngine([MakeIntelligence()]);
        var sut = new ServiceProfileViewModel("service-1", profileQuery, new StubServiceCommandService(), intelligenceEngine);
        Assert.Equal(40, sut.PopularityScore);

        intelligenceEngine.ServiceIntelligence =
        [
            new ServiceIntelligenceDto("service-1", "Haircut & Style", 72, ServicePopularityLevel.Popular, ServiceRecommendationSignal.Feature, 9, 0),
        ];
        sut.LoadCommand.Execute(null);

        Assert.Equal(72, sut.PopularityScore);
        Assert.Equal(ServicePopularityLevel.Popular, sut.PopularityLevel);
        Assert.Equal(ServiceRecommendationSignal.Feature, sut.RecommendationSignal);
    }

    [Fact]
    public void Intelligence_PropertiesRaisePropertyChangedWhenLoaded()
    {
        var intelligenceEngine = new StubIntelligenceEngine();
        var tcs = new TaskCompletionSource<ServiceProfileDto>();
        var slowProfileQuery = new StubServiceProfileQueryService((_, _) => tcs.Task);
        var sut = new ServiceProfileViewModel("service-1", slowProfileQuery, new StubServiceCommandService(), intelligenceEngine);
        var raisedProperties = new List<string>();
        sut.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is not null)
            {
                raisedProperties.Add(args.PropertyName);
            }
        };

        intelligenceEngine.ServiceIntelligence = [MakeIntelligence()];
        tcs.SetResult(MakeProfile());

        Assert.Contains(nameof(ServiceProfileViewModel.HasIntelligence), raisedProperties);
        Assert.Contains(nameof(ServiceProfileViewModel.PopularityScore), raisedProperties);
        Assert.Contains(nameof(ServiceProfileViewModel.PopularityLevel), raisedProperties);
        Assert.Contains(nameof(ServiceProfileViewModel.RecommendationSignal), raisedProperties);
    }
}
