using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Intelligence;
using Rojan.Desktop.Application.Services;
using Rojan.Desktop.Application.Specialists;
using Rojan.Desktop.Application.Specialists.Schedule;
using Rojan.Desktop.Presentation.Tests.Services;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Specialists;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

public sealed class SpecialistProfileViewModelTests
{
    /// <summary>Phase 7.2.6 Shift Engine UI Activation: an empty, no-op schedule by default - tests that only care about status/skills/intelligence don't need any schedule data configured.</summary>
    private static EmptySpecialistScheduleQueryService MakeScheduleQueryService() => new();

    private static NoOpSpecialistScheduleCommandService MakeScheduleCommandService() => new();

    private static SpecialistProfileDto MakeProfile(string specialistId = "specialist-1", IReadOnlyList<AssignedServiceDto>? assignedServices = null) =>
        new(
            new SpecialistDto(specialistId, "Jordan Lee", "Senior Colour Specialist", "jordan.lee@rojan.example", "555-0100", SpecialistStatus.Active, "Specializes in balayage."),
            [new SpecialistSkillDto("skill-1", specialistId, "Colour")],
            assignedServices ?? []);

    private static ServiceDto MakeService(string id, string name) =>
        new(id, name, ServiceCategory.Colour, ServiceStatus.Active, 60, "500,000 تومان", "Description.");

    private static SpecialistIntelligenceDto MakeIntelligence(string specialistId = "specialist-1") =>
        new(specialistId, "Jordan Lee", 55, SpecialistPerformanceLevel.Good, SpecialistRecommendationSignal.Maintain, 6, 1, 0);

    /// <summary>Specialist-Service Assignment: an empty catalog by default - tests that only care about status/skills/intelligence don't need any services configured.</summary>
    private static StubServiceQueryService MakeServiceQueryService(IReadOnlyList<ServiceDto>? catalog = null) =>
        new(_ => Task.FromResult(catalog ?? (IReadOnlyList<ServiceDto>)[]));

    [Fact]
    public void Constructor_ProfileQueryStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<SpecialistProfileDto>();
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => tcs.Task);
        var commandService = new StubSpecialistCommandService();

        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_ProfileQueryReturnsProfile_PopulatesSpecialistAndSkills()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService();

        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

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

        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void AddSkillCommand_TextIsEmpty_CanExecuteIsFalse()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService();
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

        Assert.False(sut.AddSkillCommand.CanExecute(null));

        sut.NewSkillText = "Massage";

        Assert.True(sut.AddSkillCommand.CanExecute(null));
    }

    [Fact]
    public void AddSkillCommand_Executed_CallsCommandServiceWithSpecialistIdAndNameThenClearsInput()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService();
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService())
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
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());
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
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService())
        {
            EditableStatus = SpecialistStatus.OnLeave,
        };

        sut.SaveChangesCommand.Execute(null);

        var request = Assert.Single(commandService.UpdateRequests);
        Assert.Equal("specialist-1", request.Id);
        Assert.Equal(SpecialistStatus.OnLeave, request.Status);
    }

    // Specialist Deactivation Wiring - SaveChangesAsync's success/failure handling.

    [Fact]
    public void SaveChangesCommand_ActiveToInactive_Succeeds_ClearsSaveErrorAndRefreshesProjection()
    {
        // Test Requirement 1 (deactivation succeeds) + Test Requirement 3 (projection refresh after
        // success): the profile query - the only source of truth this ViewModel has - must be re-queried
        // after a successful save, and the reloaded data (now Inactive) must be what the ViewModel shows.
        var profileCallCount = 0;
        var profileQuery = new StubSpecialistProfileQueryService((_, _) =>
        {
            profileCallCount++;
            var status = profileCallCount == 1 ? SpecialistStatus.Active : SpecialistStatus.Inactive;
            return Task.FromResult(new SpecialistProfileDto(
                new SpecialistDto("specialist-1", "Jordan Lee", "Senior Colour Specialist", "jordan.lee@rojan.example", "555-0100", status, "Specializes in balayage."),
                [],
                []));
        });
        var commandService = new StubSpecialistCommandService();
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService())
        {
            EditableStatus = SpecialistStatus.Inactive,
        };

        sut.SaveChangesCommand.Execute(null);

        Assert.Equal(2, profileCallCount); // one on construction, one after the successful save
        Assert.Equal(SpecialistStatus.Inactive, sut.Specialist?.Status);
        Assert.Equal(SpecialistStatus.Inactive, sut.EditableStatus);
        Assert.False(sut.HasSaveError);
        Assert.Null(sut.SaveErrorMessage);
    }

    [Fact]
    public void SaveChangesCommand_ActiveToInactive_Succeeds_RaisesSpecialistUpdated()
    {
        // The signal SpecialistPageViewModel relies on to keep its own directory list in sync.
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService();
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService())
        {
            EditableStatus = SpecialistStatus.Inactive,
        };
        var raised = 0;
        sut.SpecialistUpdated += (_, _) => raised++;

        sut.SaveChangesCommand.Execute(null);

        Assert.Equal(1, raised);
    }

    [Fact]
    public void SaveChangesCommand_CommandServiceThrows_SetsSafeSaveErrorMessage_NeverExposesRawExceptionDetail()
    {
        // "Do not expose internal exception details" - the message shown must be the safe, generic,
        // localized one, never the (potentially internal/developer-facing) exception message itself.
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService
        {
            UpdateSpecialistException = new NotSupportedException("ROJAN_Backend has no mutation path to change a specialist's status from Active to OnLeave."),
        };
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService())
        {
            EditableStatus = SpecialistStatus.OnLeave,
        };

        sut.SaveChangesCommand.Execute(null);

        Assert.True(sut.HasSaveError);
        Assert.NotNull(sut.SaveErrorMessage);
        Assert.DoesNotContain("ROJAN_Backend", sut.SaveErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("NotSupportedException", sut.SaveErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void SaveChangesCommand_CommandServiceThrows_LeavesFullPanelStateAndErrorMessageUntouched()
    {
        // Keep UI consistent: a save failure must never trip DashboardWidget's State/ErrorMessage
        // (Skills/Intelligence) into its Error view, which would hide the very status editor the user
        // needs to see to retry - only the new, additive SaveErrorMessage/HasSaveError may change.
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService { UpdateSpecialistException = new InvalidOperationException("boom") };
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService())
        {
            EditableStatus = SpecialistStatus.Inactive,
        };

        sut.SaveChangesCommand.Execute(null);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public void SaveChangesCommand_CommandServiceThrows_RevertsEditableStatusToLastKnownGoodValue()
    {
        // Test Requirement 4 (no local-only status mutation): a rejected status change must never be
        // left displayed as if it had been applied - EditableStatus must revert to the real, unchanged
        // backend value (Active), never stay showing the attempted (and failed) Inactive.
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile())); // MakeProfile's specialist is Active
        var commandService = new StubSpecialistCommandService { UpdateSpecialistException = new InvalidOperationException("boom") };
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService())
        {
            EditableStatus = SpecialistStatus.Inactive,
        };

        sut.SaveChangesCommand.Execute(null);

        Assert.Equal(SpecialistStatus.Active, sut.EditableStatus);
        Assert.Equal(SpecialistStatus.Active, sut.Specialist?.Status);
    }

    // Specialist-Service Assignment.

    [Fact]
    public void Constructor_ProfileHasAssignedServices_PopulatesAssignedServicesAndExcludesThemFromAvailable()
    {
        // Test Requirement 1 (load assigned services) + the picker never re-offers an already-assigned one.
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(
            MakeProfile(assignedServices: [new AssignedServiceDto("service-1", "Balayage")])));
        var catalog = new List<ServiceDto> { MakeService("service-1", "Balayage"), MakeService("service-2", "Haircut") };
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(catalog), MakeScheduleQueryService(), MakeScheduleCommandService());

        var assigned = Assert.Single(sut.AssignedServices);
        Assert.Equal("service-1", assigned.ServiceId);
        Assert.Equal("Balayage", assigned.ServiceName);

        var available = Assert.Single(sut.AvailableServicesToAssign);
        Assert.Equal("service-2", available.Id);
    }

    [Fact]
    public void AssignServiceCommand_NoSelection_CanExecuteIsFalse()
    {
        // Test Requirement 6 (no free-text assignment remains): there is no text-entry path to assignment
        // at all - the command can only ever run against a real SelectedServiceToAssign record.
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var catalog = new List<ServiceDto> { MakeService("service-1", "Balayage") };
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(catalog), MakeScheduleQueryService(), MakeScheduleCommandService());

        Assert.False(sut.AssignServiceCommand.CanExecute(null));

        sut.SelectedServiceToAssign = sut.AvailableServicesToAssign[0];

        Assert.True(sut.AssignServiceCommand.CanExecute(null));
    }

    [Fact]
    public void AssignServiceCommand_Executed_CallsCommandServiceWithRealSpecialistAndServiceIds()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var catalog = new List<ServiceDto> { MakeService("service-1", "Balayage") };
        var commandService = new StubSpecialistCommandService();
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(catalog), MakeScheduleQueryService(), MakeScheduleCommandService());
        sut.SelectedServiceToAssign = sut.AvailableServicesToAssign[0];

        sut.AssignServiceCommand.Execute(null);

        var call = Assert.Single(commandService.AssignServiceCalls);
        Assert.Equal("specialist-1", call.SpecialistId);
        Assert.Equal("service-1", call.ServiceId);
    }

    [Fact]
    public void AssignServiceCommand_Succeeds_ClearsSelectionAndRefreshesProjection()
    {
        // Test Requirement 5 (projection refresh after mutation): the profile query - the only source of
        // truth this ViewModel has - must be re-queried after a successful assignment.
        var profileCallCount = 0;
        var catalog = new List<ServiceDto> { MakeService("service-1", "Balayage") };
        var profileQuery = new StubSpecialistProfileQueryService((_, _) =>
        {
            profileCallCount++;
            var assigned = profileCallCount == 1 ? [] : new List<AssignedServiceDto> { new("service-1", "Balayage") };
            return Task.FromResult(MakeProfile(assignedServices: assigned));
        });
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, new StubSpecialistCommandService(), new StubIntelligenceEngine(), MakeServiceQueryService(catalog), MakeScheduleQueryService(), MakeScheduleCommandService());
        sut.SelectedServiceToAssign = sut.AvailableServicesToAssign[0];

        sut.AssignServiceCommand.Execute(null);

        Assert.Equal(2, profileCallCount); // one on construction, one after the successful assignment
        Assert.Null(sut.SelectedServiceToAssign);
        Assert.Contains(sut.AssignedServices, assignment => assignment.ServiceId == "service-1");
        Assert.False(sut.HasAssignmentError);
    }

    [Fact]
    public void AssignServiceCommand_CommandServiceThrows_SetsSafeAssignmentErrorMessage_NeverExposesRawExceptionDetail()
    {
        // Test Requirement 4 (Backend failure does not corrupt UI state), safe-error half.
        var catalog = new List<ServiceDto> { MakeService("service-1", "Balayage") };
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService { AssignServiceException = new ApiException("Failed to assign service 'service-1' (status 500): Server error") };
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(catalog), MakeScheduleQueryService(), MakeScheduleCommandService());
        sut.SelectedServiceToAssign = sut.AvailableServicesToAssign[0];

        sut.AssignServiceCommand.Execute(null);

        Assert.True(sut.HasAssignmentError);
        Assert.NotNull(sut.AssignmentErrorMessage);
        Assert.DoesNotContain("status 500", sut.AssignmentErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void AssignServiceCommand_CommandServiceThrows_LeavesAssignedServicesAndAvailableServicesUnchanged()
    {
        // Test Requirement 4 (Backend failure does not corrupt UI state), no-corruption half: since
        // AssignedServices/AvailableServicesToAssign are only ever mutated inside LoadAsync (which only
        // runs after a confirmed success), a rejected assignment must leave both exactly as they were.
        var catalog = new List<ServiceDto> { MakeService("service-1", "Balayage"), MakeService("service-2", "Haircut") };
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService { AssignServiceException = new InvalidOperationException("boom") };
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(catalog), MakeScheduleQueryService(), MakeScheduleCommandService());
        sut.SelectedServiceToAssign = sut.AvailableServicesToAssign[0];

        sut.AssignServiceCommand.Execute(null);

        Assert.Empty(sut.AssignedServices);
        Assert.Equal(2, sut.AvailableServicesToAssign.Count);
    }

    [Fact]
    public void RemoveServiceAssignmentCommand_Executed_CallsCommandServiceWithRealIdsAndRefreshesProjection()
    {
        var profileCallCount = 0;
        var profileQuery = new StubSpecialistProfileQueryService((_, _) =>
        {
            profileCallCount++;
            var assigned = profileCallCount == 1 ? new List<AssignedServiceDto> { new("service-1", "Balayage") } : [];
            return Task.FromResult(MakeProfile(assignedServices: assigned));
        });
        var commandService = new StubSpecialistCommandService();
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());
        var assignment = sut.AssignedServices[0];

        sut.RemoveServiceAssignmentCommand.Execute(assignment);

        var call = Assert.Single(commandService.RemoveServiceAssignmentCalls);
        Assert.Equal("specialist-1", call.SpecialistId);
        Assert.Equal("service-1", call.ServiceId);
        Assert.Equal(2, profileCallCount);
        Assert.Empty(sut.AssignedServices);
    }

    [Fact]
    public void RemoveServiceAssignmentCommand_CommandServiceThrows_SetsSafeErrorAndLeavesAssignedServicesUnchanged()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(
            MakeProfile(assignedServices: [new AssignedServiceDto("service-1", "Balayage")])));
        var commandService = new StubSpecialistCommandService { RemoveServiceAssignmentException = new InvalidOperationException("boom") };
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService, new StubIntelligenceEngine(), MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());
        var assignment = sut.AssignedServices[0];

        sut.RemoveServiceAssignmentCommand.Execute(assignment);

        Assert.True(sut.HasAssignmentError);
        Assert.Single(sut.AssignedServices); // unchanged - the failed removal never took effect locally
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

        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, new StubSpecialistCommandService(), intelligenceEngine, MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

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

        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, new StubSpecialistCommandService(), intelligenceEngine, MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

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

        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, new StubSpecialistCommandService(), intelligenceEngine, MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.False(sut.HasIntelligence);
    }

    [Fact]
    public void LoadCommand_ExecutedAfterIntelligenceChanges_RefreshesIntelligenceProperties()
    {
        // Refresh behavior: a reload must pick up the engine's latest data, not cache the first result.
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var intelligenceEngine = new StubIntelligenceEngine([MakeIntelligence()]);
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, new StubSpecialistCommandService(), intelligenceEngine, MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());
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
        var sut = new SpecialistProfileViewModel("specialist-1", slowProfileQuery, new StubSpecialistCommandService(), intelligenceEngine, MakeServiceQueryService(), MakeScheduleQueryService(), MakeScheduleCommandService());
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
