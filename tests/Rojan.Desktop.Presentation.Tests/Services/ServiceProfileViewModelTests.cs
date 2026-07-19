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

    [Fact]
    public void Constructor_ProfileQueryStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<ServiceProfileDto>();
        var profileQuery = new StubServiceProfileQueryService((_, _) => tcs.Task);
        var commandService = new StubServiceCommandService();

        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService);

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_ProfileQueryReturnsProfile_PopulatesServiceAndAssignedSpecialists()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubServiceCommandService();

        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal("Haircut & Style", sut.Service?.Name);
        Assert.Single(sut.AssignedSpecialists);
    }

    [Fact]
    public void Constructor_ProfileQueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromException<ServiceProfileDto>(new InvalidOperationException("boom")));
        var commandService = new StubServiceCommandService();

        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void AssignSpecialistCommand_NameIsEmpty_CanExecuteIsFalse()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubServiceCommandService();
        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService);

        Assert.False(sut.AssignSpecialistCommand.CanExecute(null));

        sut.NewSpecialistName = "Casey Morgan";

        Assert.True(sut.AssignSpecialistCommand.CanExecute(null));
    }

    [Fact]
    public void AssignSpecialistCommand_Executed_CallsCommandServiceWithServiceIdAndNameThenClearsInput()
    {
        var profileQuery = new StubServiceProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubServiceCommandService();
        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService)
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
        var sut = new ServiceProfileViewModel("service-1", profileQuery, commandService);
        var assignment = new AssignedSpecialistDto("assignment-1", "service-1", "specialist-1", "Jordan Lee");

        sut.UnassignSpecialistCommand.Execute(assignment);

        var call = Assert.Single(commandService.UnassignCalls);
        Assert.Equal("service-1", call.ServiceId);
        Assert.Equal("assignment-1", call.AssignmentId);
    }
}
