using Rojan.Desktop.Application.HR;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

public sealed class EmployeeProfileViewModelTests
{
    private static EmployeeDto MakeEmployee(string id, EmployeeStatus status = EmployeeStatus.Active) =>
        new(id, string.Empty, "Jordan Lee", "jordan.lee@rojan.example", "+1 555", EmployeeRole.Colorist, Department.Hair, EmploymentType.FullTime, status, DateOnly.FromDateTime(DateTime.Today), 3200m);

    private static EmployeeProfileDto MakeProfile(string id, EmployeeStatus status = EmployeeStatus.Active) =>
        new(MakeEmployee(id, status), null, [], [], [], []);

    [Fact]
    public void Constructor_ProfileLoads_StateIsLoaded()
    {
        var queryService = new StubEmployeeQueryService(
            _ => Task.FromResult<IReadOnlyList<EmployeeDto>>([]),
            getProfile: (employeeId, _) => Task.FromResult(MakeProfile(employeeId)));

        var sut = new EmployeeProfileViewModel("employee-1", queryService, new StubEmployeeCommandService());

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal("employee-1", sut.Profile?.Employee.Id);
    }

    [Fact]
    public void Constructor_ProfileQueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubEmployeeQueryService(
            _ => Task.FromResult<IReadOnlyList<EmployeeDto>>([]),
            getProfile: (_, _) => Task.FromException<EmployeeProfileDto>(new InvalidOperationException("boom")));

        var sut = new EmployeeProfileViewModel("employee-1", queryService, new StubEmployeeCommandService());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void ActivateCommand_Executed_CallsCommandServiceReloadsAndInvokesOnChanged()
    {
        var queryService = new StubEmployeeQueryService(
            _ => Task.FromResult<IReadOnlyList<EmployeeDto>>([]),
            getProfile: (employeeId, _) => Task.FromResult(MakeProfile(employeeId)));
        var commandService = new StubEmployeeCommandService();
        var changed = false;
        var sut = new EmployeeProfileViewModel("employee-1", queryService, commandService, () => changed = true);

        sut.ActivateCommand.Execute(null);

        var call = Assert.Single(commandService.ActivatedIds);
        Assert.Equal("employee-1", call);
        Assert.True(changed);
    }

    [Fact]
    public void SuspendCommand_Executed_CallsCommandService()
    {
        var queryService = new StubEmployeeQueryService(
            _ => Task.FromResult<IReadOnlyList<EmployeeDto>>([]),
            getProfile: (employeeId, _) => Task.FromResult(MakeProfile(employeeId)));
        var commandService = new StubEmployeeCommandService();
        var sut = new EmployeeProfileViewModel("employee-1", queryService, commandService);

        sut.SuspendCommand.Execute(null);

        Assert.Single(commandService.SuspendedIds);
    }
}
