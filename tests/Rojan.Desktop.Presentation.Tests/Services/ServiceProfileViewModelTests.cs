using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Intelligence;
using Rojan.Desktop.Application.Services;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Services;

namespace Rojan.Desktop.Presentation.Tests.Services;

public sealed class ServiceProfileViewModelTests
{
    private const string Secret = "Haircut & Style / $65 / Classic cut and blow-dry finish.";
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
    public void Constructor_ProfileQueryThrows_StateIsErrorAndSetsGenericErrorMessage()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromException<ServiceProfileDto>(new InvalidOperationException("boom: price 45.00 / commission 15%")));
        var commandService = new StubServiceCommandService();

        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService, new StubIntelligenceEngine());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.DoesNotContain("45.00", sut.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
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
    public void AssignSpecialistCommand_BackendThrows_SetsInlineSaveError_DoesNotThrow_PreservesInput_LogsOperationOnly()
    {
        const string backendBody = "HTTP 500: backend response body";
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubServiceCommandService { AssignSpecialistException = new InvalidOperationException(backendBody) };
        var logger = new RecordingLogger<ServiceProfileViewModel>();
        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService, new StubIntelligenceEngine(), logger)
        {
            NewSpecialistName = "Casey Morgan",
        };

        var exception = Record.Exception(() => sut.AssignSpecialistCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasSaveError);
        Assert.Equal(Strings.Services_SaveError, sut.SaveErrorMessage);
        Assert.Equal("Casey Morgan", sut.NewSpecialistName); // input preserved for retry
        var entry = Assert.Single(logger.Entries.FindAll(e => e.Message.Contains("AssignSpecialistAsync", StringComparison.Ordinal)));
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.DoesNotContain(backendBody, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnassignSpecialistCommand_BackendThrows_SetsInlineSaveError_DoesNotThrow()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubServiceCommandService { UnassignSpecialistException = new InvalidOperationException("boom") };
        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService, new StubIntelligenceEngine());
        var assignment = new AssignedSpecialistDto("assignment-1", "service-1", "specialist-1", "Jordan Lee");

        var exception = Record.Exception(() => sut.UnassignSpecialistCommand.Execute(assignment));

        Assert.Null(exception);
        Assert.True(sut.HasSaveError);
        Assert.Equal(Strings.Services_SaveError, sut.SaveErrorMessage);
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

    [Fact]
    public void LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromException<ServiceProfileDto>(new InvalidOperationException(Secret)));
        var logger = new RecordingLogger<ServiceProfileViewModel>();

        var sut = new ServiceProfileViewModel("service-1", profileQuery, new StubServiceCommandService(), new StubIntelligenceEngine(), logger);

        Assert.Equal(DashboardState.Error, sut.State);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=LoadAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SaveChangesCommand_Failure_LogsErrorWithOperationNameOnly_AndStillRevertsBuffers()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubServiceCommandService { UpdateServiceException = new InvalidOperationException(Secret) };
        var logger = new RecordingLogger<ServiceProfileViewModel>();
        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService, new StubIntelligenceEngine(), logger);
        sut.EditableName = "Edited name that should be reverted";

        sut.SaveChangesCommand.Execute(null);

        Assert.True(sut.HasSaveError);
        Assert.Equal("Haircut & Style", sut.EditableName);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=SaveChangesAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DeactivateCommand_Failure_LogsErrorWithOperationNameOnly_NoLeak()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubServiceCommandService { UpdateServiceException = new InvalidOperationException(Secret) };
        var logger = new RecordingLogger<ServiceProfileViewModel>();
        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService, new StubIntelligenceEngine(), logger);

        sut.DeactivateCommand.Execute(null);

        Assert.True(sut.HasSaveError);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=DeactivateAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows_AndSurfacesGenericMessage()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromException<ServiceProfileDto>(new InvalidOperationException("boom")));

        var sut = new ServiceProfileViewModel("service-1", profileQuery, new StubServiceCommandService(), new StubIntelligenceEngine());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
    }
}
