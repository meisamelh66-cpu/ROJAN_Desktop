using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.HR;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

public sealed class EmployeeProfileViewModelTests
{
    private const string PiiSecret = "Jordan Lee / jordan.lee@rojan.example / +1 555 / salary 3200";

    [Fact]
    public void LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoPiiLeak()
    {
        var queryService = new StubEmployeeQueryService(
            _ => Task.FromResult<IReadOnlyList<EmployeeDto>>([]),
            getProfile: (_, _) => Task.FromException<EmployeeProfileDto>(new InvalidOperationException(PiiSecret)));
        var logger = new RecordingLogger<EmployeeProfileViewModel>();

        var sut = new EmployeeProfileViewModel("employee-1", queryService, new StubEmployeeCommandService(), onChanged: null, logger);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.DoesNotContain(PiiSecret, sut.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=LoadAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(PiiSecret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows_AndSurfacesGenericMessage()
    {
        var queryService = new StubEmployeeQueryService(
            _ => Task.FromResult<IReadOnlyList<EmployeeDto>>([]),
            getProfile: (_, _) => Task.FromException<EmployeeProfileDto>(new InvalidOperationException("boom")));

        var sut = new EmployeeProfileViewModel("employee-1", queryService, new StubEmployeeCommandService());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
    }

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
    public void Constructor_ProfileQueryThrows_StateIsErrorAndSetsGenericErrorMessage()
    {
        var queryService = new StubEmployeeQueryService(
            _ => Task.FromResult<IReadOnlyList<EmployeeDto>>([]),
            getProfile: (_, _) => Task.FromException<EmployeeProfileDto>(new InvalidOperationException("boom")));

        var sut = new EmployeeProfileViewModel("employee-1", queryService, new StubEmployeeCommandService());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
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

    // ---------------------------------------------------------------------
    // Production Hardening - Missing-Guard Sweep Wave B. Activate/Deactivate/
    // Suspend now surface a backend failure via ActionErrorMessage/
    // HasActionError instead of the global dialog; the profile, State and the
    // onChanged callback are left untouched on failure, and logging is
    // operation-name-only (no PII / salary leak).
    // ---------------------------------------------------------------------

    private static StubEmployeeQueryService LoadingQueryService() =>
        new(_ => Task.FromResult<IReadOnlyList<EmployeeDto>>([]), getProfile: (employeeId, _) => Task.FromResult(MakeProfile(employeeId)));

    [Fact]
    public void ActivateCommand_Failure_DoesNotThrow_SetsActionErrorAndPreservesStateAndOnChanged()
    {
        var commandService = new StubEmployeeCommandService { ActivateEmployeeException = new InvalidOperationException(PiiSecret) };
        var changed = false;
        var sut = new EmployeeProfileViewModel("employee-1", LoadingQueryService(), commandService, () => changed = true);

        var exception = Record.Exception(() => sut.ActivateCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal("employee-1", sut.Profile?.Employee.Id);
        Assert.False(changed);
        Assert.Single(commandService.ActivatedIds);
    }

    [Fact]
    public void DeactivateCommand_Failure_DoesNotThrow_SetsActionError()
    {
        var commandService = new StubEmployeeCommandService { DeactivateEmployeeException = new InvalidOperationException(PiiSecret) };
        var sut = new EmployeeProfileViewModel("employee-1", LoadingQueryService(), commandService);

        var exception = Record.Exception(() => sut.DeactivateCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
    }

    [Fact]
    public void SuspendCommand_Failure_DoesNotThrow_SetsActionError()
    {
        var commandService = new StubEmployeeCommandService { SuspendEmployeeException = new InvalidOperationException(PiiSecret) };
        var sut = new EmployeeProfileViewModel("employee-1", LoadingQueryService(), commandService);

        var exception = Record.Exception(() => sut.SuspendCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
    }

    [Fact]
    public void ActivateCommand_Failure_LogsOperationNameOnly_NoPiiLeak()
    {
        var commandService = new StubEmployeeCommandService { ActivateEmployeeException = new InvalidOperationException(PiiSecret) };
        var logger = new RecordingLogger<EmployeeProfileViewModel>();
        var sut = new EmployeeProfileViewModel("employee-1", LoadingQueryService(), commandService, onChanged: null, logger);

        sut.ActivateCommand.Execute(null);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("Operation=ActivateAsync", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains(PiiSecret, StringComparison.Ordinal));
        Assert.DoesNotContain(PiiSecret, sut.ActionErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void ActivateCommand_SuccessAfterFailure_ClearsActionErrorAndInvokesOnChanged()
    {
        var commandService = new StubEmployeeCommandService { ActivateEmployeeException = new InvalidOperationException("boom") };
        var changed = false;
        var sut = new EmployeeProfileViewModel("employee-1", LoadingQueryService(), commandService, () => changed = true);
        sut.ActivateCommand.Execute(null);
        Assert.True(sut.HasActionError);

        commandService.ActivateEmployeeException = null;
        sut.ActivateCommand.Execute(null);

        Assert.False(sut.HasActionError);
        Assert.Null(sut.ActionErrorMessage);
        Assert.True(changed);
    }
}
