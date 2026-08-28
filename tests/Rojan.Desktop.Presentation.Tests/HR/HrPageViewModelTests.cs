using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.HR;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

public sealed class HrPageViewModelTests
{
    private static EmployeeDto MakeEmployee(string id, string fullName) =>
        new(id, string.Empty, fullName, $"{id}@rojan.example", "+1 555", EmployeeRole.Stylist, Department.Hair, EmploymentType.FullTime, EmployeeStatus.Active, DateOnly.FromDateTime(DateTime.Today), 2500m);

    private static EmployeeProfileDto MakeProfile(string id) =>
        new(MakeEmployee(id, "Test Employee"), null, [], [], [], []);

    private static HrPageViewModel MakeSut(
        StubEmployeeQueryService? employeeQueryService = null,
        StubEmployeeCommandService? employeeCommandService = null,
        StubAttendanceQueryService? attendanceQueryService = null,
        StubAttendanceCommandService? attendanceCommandService = null,
        StubShiftQueryService? shiftQueryService = null,
        StubShiftCommandService? shiftCommandService = null,
        StubCommissionQueryService? commissionQueryService = null,
        StubCommissionCommandService? commissionCommandService = null,
        StubPayrollQueryService? payrollQueryService = null,
        StubPayrollCommandService? payrollCommandService = null,
        RecordingLogger<HrPageViewModel>? logger = null,
        RecordingLoggerFactory? loggerFactory = null) => new(
        employeeQueryService ?? new StubEmployeeQueryService(_ => Task.FromResult<IReadOnlyList<EmployeeDto>>([]), getProfile: (id, _) => Task.FromResult(MakeProfile(id))),
        employeeCommandService ?? new StubEmployeeCommandService(),
        attendanceQueryService ?? new StubAttendanceQueryService(),
        attendanceCommandService ?? new StubAttendanceCommandService(),
        shiftQueryService ?? new StubShiftQueryService(),
        shiftCommandService ?? new StubShiftCommandService(),
        commissionQueryService ?? new StubCommissionQueryService(),
        commissionCommandService ?? new StubCommissionCommandService(),
        payrollQueryService ?? new StubPayrollQueryService(),
        payrollCommandService ?? new StubPayrollCommandService(),
        logger,
        loggerFactory);

    [Fact]
    public void LoggerFactory_ForwardedToEmployeeProfileChild_ChildLoadFailureIsLoggedViaTheFactory()
    {
        const string secret = "child employee pii / salary 2500 / +1 555";
        var employees = new List<EmployeeDto> { MakeEmployee("employee-1", "Jordan Lee") };
        var queryService = new StubEmployeeQueryService(
            _ => Task.FromResult<IReadOnlyList<EmployeeDto>>(employees),
            getProfile: (_, _) => Task.FromException<EmployeeProfileDto>(new InvalidOperationException(secret)));
        var loggerFactory = new RecordingLoggerFactory();

        var sut = MakeSut(queryService, loggerFactory: loggerFactory);

        Assert.NotNull(sut.Profile);
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains(nameof(EmployeeProfileViewModel), entry.Category, StringComparison.Ordinal);
        Assert.Contains("Operation=LoadAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_EmployeesLoad_StateIsLoadedAndSelectsFirstEmployee()
    {
        var employees = new List<EmployeeDto> { MakeEmployee("employee-1", "Jordan Lee") };
        var queryService = new StubEmployeeQueryService(_ => Task.FromResult<IReadOnlyList<EmployeeDto>>(employees), getProfile: (id, _) => Task.FromResult(MakeProfile(id)));

        var sut = MakeSut(queryService);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(employees, sut.Employees);
        Assert.Equal(employees[0], sut.SelectedEmployee);
        Assert.NotNull(sut.Profile);
    }

    [Fact]
    public void Constructor_EmployeesEmpty_StateIsEmpty()
    {
        var queryService = new StubEmployeeQueryService(_ => Task.FromResult<IReadOnlyList<EmployeeDto>>([]));

        var sut = MakeSut(queryService);

        Assert.Equal(DashboardState.Empty, sut.State);
        Assert.Null(sut.SelectedEmployee);
    }

    [Fact]
    public void Constructor_QueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubEmployeeQueryService(_ => Task.FromException<IReadOnlyList<EmployeeDto>>(new InvalidOperationException("boom")));

        var sut = MakeSut(queryService);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    // Phase 8.19 Logging Wave 2A: LoadAsync / SearchAsync now log at Error before
    // their existing handling - user-visible behaviour unchanged.

    [Fact]
    public void LoadAsync_QueryThrows_LogsError()
    {
        var queryService = new StubEmployeeQueryService(_ => Task.FromException<IReadOnlyList<EmployeeDto>>(new InvalidOperationException("boom")));
        var logger = new RecordingLogger<HrPageViewModel>();

        var sut = MakeSut(queryService, logger: logger);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("LoadAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows()
    {
        var queryService = new StubEmployeeQueryService(_ => Task.FromException<IReadOnlyList<EmployeeDto>>(new InvalidOperationException("boom")));

        var exception = Record.Exception(() => MakeSut(queryService));

        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_LoadsDashboardSummary()
    {
        var queryService = new StubEmployeeQueryService(
            _ => Task.FromResult<IReadOnlyList<EmployeeDto>>([]),
            getDashboardSummary: _ => Task.FromResult(new HrDashboardSummaryDto(20, 5, 2, 1, 15000m, 800m, 92.5m)));

        var sut = MakeSut(queryService);

        Assert.NotNull(sut.Summary);
        Assert.Equal(20, sut.Summary.EmployeeCount);
        Assert.Equal(92.5m, sut.Summary.AverageAttendancePercent);
    }

    [Fact]
    public void SelectSectionCommand_Executed_ChangesSelectedSection()
    {
        var sut = MakeSut();

        sut.SelectSectionCommand.Execute(HrSection.Commission);

        Assert.Equal(HrSection.Commission, sut.SelectedSection);
    }

    [Fact]
    public void SearchText_MatchesEmployeeName_FiltersToMatchingEmployeesOnly()
    {
        var employees = new List<EmployeeDto> { MakeEmployee("employee-1", "Jordan Lee"), MakeEmployee("employee-2", "Priya Nair") };
        var queryService = new StubEmployeeQueryService(_ => Task.FromResult<IReadOnlyList<EmployeeDto>>(employees), getProfile: (id, _) => Task.FromResult(MakeProfile(id)));
        var sut = MakeSut(queryService);

        sut.SearchText = "priya";

        Assert.Equal(["employee-2"], sut.Employees.Select(e => e.Id));
    }

    [Fact]
    public void CreateEmployeeCommand_CanExecute_RequiresFullNameAndEmail()
    {
        var sut = MakeSut();

        Assert.False(sut.CreateEmployeeCommand.CanExecute(null));

        sut.NewEmployeeFullName = "New Hire";
        sut.NewEmployeeEmail = "new.hire@rojan.example";

        Assert.True(sut.CreateEmployeeCommand.CanExecute(null));
    }

    [Fact]
    public void CreateEmployeeCommand_Executed_CallsCommandServiceAndClearsForm()
    {
        var commandService = new StubEmployeeCommandService();
        var sut = MakeSut(employeeCommandService: commandService);
        sut.NewEmployeeFullName = "New Hire";
        sut.NewEmployeeEmail = "new.hire@rojan.example";
        sut.NewEmployeeBaseSalary = "2200";

        sut.CreateEmployeeCommand.Execute(null);

        var request = Assert.Single(commandService.CreateRequests);
        Assert.Equal("New Hire", request.FullName);
        Assert.Equal(2200m, request.BaseSalary);
        Assert.Equal(string.Empty, sut.NewEmployeeFullName);
    }

    [Fact]
    public void RecordAttendanceCommand_CanExecute_RequiresSelectedEmployee()
    {
        var sut = MakeSut();

        Assert.False(sut.RecordAttendanceCommand.CanExecute(null));

        sut.AttendanceSelectedEmployee = MakeEmployee("employee-1", "Jordan Lee");

        Assert.True(sut.RecordAttendanceCommand.CanExecute(null));
    }

    [Fact]
    public void RecordAttendanceCommand_Executed_CallsCommandServiceWithParsedCheckInTime()
    {
        var commandService = new StubAttendanceCommandService();
        var sut = MakeSut(attendanceCommandService: commandService);
        sut.AttendanceSelectedEmployee = MakeEmployee("employee-1", "Jordan Lee");
        sut.AttendanceCheckInTime = "09:15";

        sut.RecordAttendanceCommand.Execute(null);

        var request = Assert.Single(commandService.RecordRequests);
        Assert.Equal("employee-1", request.EmployeeId);
        Assert.Equal(new TimeSpan(9, 15, 0), request.CheckInTime);
    }

    [Fact]
    public void CreateShiftCommand_CanExecute_RequiresLabelAndTimes()
    {
        var sut = MakeSut();

        Assert.False(sut.CreateShiftCommand.CanExecute(null));

        sut.NewShiftLabel = "Morning - Hair";
        sut.NewShiftStartTime = "09:00";
        sut.NewShiftEndTime = "17:00";

        Assert.True(sut.CreateShiftCommand.CanExecute(null));
    }

    [Fact]
    public void CreateShiftCommand_Executed_AddsShiftToCollection()
    {
        var sut = MakeSut();
        sut.NewShiftLabel = "Morning - Hair";
        sut.NewShiftStartTime = "09:00";
        sut.NewShiftEndTime = "17:00";

        sut.CreateShiftCommand.Execute(null);

        Assert.Single(sut.Shifts);
    }

    [Fact]
    public void AssignShiftCommand_Executed_AddsAssignmentToCollection()
    {
        var sut = MakeSut();
        sut.AssignShiftSelectedShift = new ShiftDto("shift-1", "Morning - Hair", Department.Hair, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0));
        sut.AssignShiftSelectedEmployee = MakeEmployee("employee-1", "Jordan Lee");

        sut.AssignShiftCommand.Execute(null);

        Assert.Single(sut.ShiftAssignments);
    }

    [Fact]
    public void RequestLeaveCommand_CanExecute_RequiresEmployeeAndReason()
    {
        var sut = MakeSut();

        Assert.False(sut.RequestLeaveCommand.CanExecute(null));

        sut.LeaveSelectedEmployee = MakeEmployee("employee-1", "Jordan Lee");
        sut.LeaveReason = "Vacation";

        Assert.True(sut.RequestLeaveCommand.CanExecute(null));
    }

    [Fact]
    public void RequestLeaveCommand_Executed_InsertsIntoLeaveRequests()
    {
        var sut = MakeSut();
        sut.LeaveSelectedEmployee = MakeEmployee("employee-1", "Jordan Lee");
        sut.LeaveReason = "Vacation";

        sut.RequestLeaveCommand.Execute(null);

        Assert.Single(sut.LeaveRequests);
    }

    [Fact]
    public void ApproveLeaveCommand_Executed_CallsCommandService()
    {
        var commandService = new StubAttendanceCommandService();
        var sut = MakeSut(attendanceCommandService: commandService);
        var leaveRequest = new LeaveRequestDto("leave-1", "employee-1", "Jordan Lee", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today), "Vacation", LeaveStatus.Pending, DateTimeOffset.Now);

        sut.ApproveLeaveCommand.Execute(leaveRequest);

        Assert.Single(commandService.ApprovedLeaveIds);
    }

    [Fact]
    public void CreateCommissionRuleCommand_CanExecute_RequiresEmployeeAndValue()
    {
        var sut = MakeSut();

        Assert.False(sut.CreateCommissionRuleCommand.CanExecute(null));

        sut.NewRuleSelectedEmployee = MakeEmployee("employee-1", "Jordan Lee");
        sut.NewRuleValue = "0.10";

        Assert.True(sut.CreateCommissionRuleCommand.CanExecute(null));
    }

    [Fact]
    public void GenerateCommissionsCommand_Executed_AppendsGeneratedTransactionsAndSetsStatusMessage()
    {
        var generated = new List<CommissionTransactionDto>
        {
            new("commission-new", "employee-1", "Jordan Lee", "invoice-8", "Corporate Group Styling", 518.40m, 62.21m, DateTimeOffset.Now),
        };
        var commandService = new StubCommissionCommandService(_ => Task.FromResult<IReadOnlyList<CommissionTransactionDto>>(generated));
        var sut = MakeSut(commissionCommandService: commandService);

        sut.GenerateCommissionsCommand.Execute(null);

        Assert.Contains(sut.CommissionTransactions, t => t.Id == "commission-new");
        Assert.Equal("Generated 1 new commission from Accounting.", sut.StatusMessage);
    }

    [Fact]
    public void GeneratePayrollCommand_CanExecute_RequiresSelectedEmployee()
    {
        var sut = MakeSut();

        Assert.False(sut.GeneratePayrollCommand.CanExecute(null));

        sut.PayrollSelectedEmployee = MakeEmployee("employee-1", "Jordan Lee");

        Assert.True(sut.GeneratePayrollCommand.CanExecute(null));
    }

    [Fact]
    public void GeneratePayrollCommand_Executed_InsertsIntoPayrollSummaries()
    {
        var sut = MakeSut();
        sut.PayrollSelectedEmployee = MakeEmployee("employee-1", "Jordan Lee");
        sut.PayrollBonus = "100";
        sut.PayrollDeduction = "50";

        sut.GeneratePayrollCommand.Execute(null);

        Assert.Single(sut.PayrollSummaries);
    }

    // ---------------------------------------------------------------------
    // Production Hardening - Missing-Guard Sweep Wave B (HR commands).
    // Every user-triggered HR write command now surfaces a backend failure
    // via the non-destructive ActionErrorMessage/HasActionError pair instead
    // of the global App.DispatcherUnhandledException dialog. Failures never
    // expose Exception.Message, backend payload, salary/commission data or
    // employee PII, and log operation-name-only via the existing
    // LogOperationFailed([LoggerMessage]).
    // ---------------------------------------------------------------------

    private const string HrBackendSecret = "backend 500: employee Jordan Lee salary=3200 net=2870 commission=518.40 ssn=123";

    [Fact]
    public void CreateEmployeeCommand_Failure_DoesNotThrow_SetsActionErrorAndPreservesForm()
    {
        var commandService = new StubEmployeeCommandService { CreateEmployeeException = new InvalidOperationException(HrBackendSecret) };
        var sut = MakeSut(employeeCommandService: commandService);
        sut.NewEmployeeFullName = "New Hire";
        sut.NewEmployeeEmail = "new.hire@rojan.example";
        sut.NewEmployeeBaseSalary = "2200";

        var exception = Record.Exception(() => sut.CreateEmployeeCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.NotEqual(DashboardState.Error, sut.State);
        Assert.Equal("New Hire", sut.NewEmployeeFullName);
        Assert.Equal("new.hire@rojan.example", sut.NewEmployeeEmail);
        Assert.Single(commandService.CreateRequests);
    }

    [Fact]
    public void RecordAttendanceCommand_Failure_DoesNotThrow_SetsActionErrorAndPreservesForm()
    {
        var commandService = new StubAttendanceCommandService { RecordAttendanceException = new InvalidOperationException(HrBackendSecret) };
        var sut = MakeSut(attendanceCommandService: commandService);
        sut.AttendanceSelectedEmployee = MakeEmployee("employee-1", "Jordan Lee");
        sut.AttendanceCheckInTime = "09:15";
        sut.AttendanceNotes = "late shuttle";

        var exception = Record.Exception(() => sut.RecordAttendanceCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.Equal("09:15", sut.AttendanceCheckInTime);
        Assert.Equal("late shuttle", sut.AttendanceNotes);
    }

    [Fact]
    public void CreateShiftCommand_Failure_DoesNotThrow_SetsActionErrorAndDoesNotAddShift()
    {
        var commandService = new StubShiftCommandService { CreateShiftException = new InvalidOperationException(HrBackendSecret) };
        var sut = MakeSut(shiftCommandService: commandService);
        sut.NewShiftLabel = "Morning - Hair";
        sut.NewShiftStartTime = "09:00";
        sut.NewShiftEndTime = "17:00";

        var exception = Record.Exception(() => sut.CreateShiftCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.Empty(sut.Shifts);
        Assert.Equal("Morning - Hair", sut.NewShiftLabel);
    }

    [Fact]
    public void AssignShiftCommand_Failure_DoesNotThrow_SetsActionErrorAndDoesNotAddAssignment()
    {
        var commandService = new StubShiftCommandService { AssignShiftException = new InvalidOperationException(HrBackendSecret) };
        var sut = MakeSut(shiftCommandService: commandService);
        sut.AssignShiftSelectedShift = new ShiftDto("shift-1", "Morning - Hair", Department.Hair, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0));
        sut.AssignShiftSelectedEmployee = MakeEmployee("employee-1", "Jordan Lee");

        var exception = Record.Exception(() => sut.AssignShiftCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Empty(sut.ShiftAssignments);
    }

    [Fact]
    public void RequestLeaveCommand_Failure_DoesNotThrow_SetsActionErrorAndPreservesReason()
    {
        var commandService = new StubAttendanceCommandService { RequestLeaveException = new InvalidOperationException(HrBackendSecret) };
        var sut = MakeSut(attendanceCommandService: commandService);
        sut.LeaveSelectedEmployee = MakeEmployee("employee-1", "Jordan Lee");
        sut.LeaveReason = "Vacation";

        var exception = Record.Exception(() => sut.RequestLeaveCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.Empty(sut.LeaveRequests);
        Assert.Equal("Vacation", sut.LeaveReason);
    }

    [Fact]
    public void ApproveLeaveCommand_Failure_DoesNotThrow_SetsActionErrorAndLeavesRowUnchanged()
    {
        var commandService = new StubAttendanceCommandService { ApproveLeaveException = new InvalidOperationException(HrBackendSecret) };
        var sut = MakeSut(attendanceCommandService: commandService);
        var leaveRequest = new LeaveRequestDto("leave-1", "employee-1", "Jordan Lee", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today), "Vacation", LeaveStatus.Pending, DateTimeOffset.Now);
        sut.LeaveRequests.Add(leaveRequest);

        var exception = Record.Exception(() => sut.ApproveLeaveCommand.Execute(leaveRequest));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(LeaveStatus.Pending, Assert.Single(sut.LeaveRequests).Status);
        Assert.Single(commandService.ApprovedLeaveIds);
    }

    [Fact]
    public void RejectLeaveCommand_Failure_DoesNotThrow_SetsActionError()
    {
        var commandService = new StubAttendanceCommandService { RejectLeaveException = new InvalidOperationException(HrBackendSecret) };
        var sut = MakeSut(attendanceCommandService: commandService);
        var leaveRequest = new LeaveRequestDto("leave-1", "employee-1", "Jordan Lee", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today), "Vacation", LeaveStatus.Pending, DateTimeOffset.Now);
        sut.LeaveRequests.Add(leaveRequest);

        var exception = Record.Exception(() => sut.RejectLeaveCommand.Execute(leaveRequest));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
    }

    [Fact]
    public void CreateCommissionRuleCommand_Failure_DoesNotThrow_SetsActionErrorAndPreservesForm()
    {
        var commandService = new StubCommissionCommandService { CreateCommissionRuleException = new InvalidOperationException(HrBackendSecret) };
        var sut = MakeSut(commissionCommandService: commandService);
        sut.NewRuleSelectedEmployee = MakeEmployee("employee-1", "Jordan Lee");
        sut.NewRuleValue = "0.10";
        sut.NewRuleDescription = "Colour upsell";

        var exception = Record.Exception(() => sut.CreateCommissionRuleCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Empty(sut.CommissionRules);
        Assert.Equal("0.10", sut.NewRuleValue);
        Assert.Equal("Colour upsell", sut.NewRuleDescription);
    }

    [Fact]
    public void GenerateCommissionsCommand_Failure_DoesNotThrow_SetsActionErrorAndLeavesStatusMessageUntouched()
    {
        var commandService = new StubCommissionCommandService(_ => Task.FromException<IReadOnlyList<CommissionTransactionDto>>(new InvalidOperationException(HrBackendSecret)));
        var sut = MakeSut(commissionCommandService: commandService);

        var exception = Record.Exception(() => sut.GenerateCommissionsCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.Empty(sut.CommissionTransactions);
        Assert.Equal(string.Empty, sut.StatusMessage);
    }

    [Fact]
    public void GeneratePayrollCommand_Failure_DoesNotThrow_SetsActionErrorAndDoesNotInsertSummary()
    {
        var commandService = new StubPayrollCommandService { GeneratePayrollException = new InvalidOperationException(HrBackendSecret) };
        var sut = MakeSut(payrollCommandService: commandService);
        sut.PayrollSelectedEmployee = MakeEmployee("employee-1", "Jordan Lee");
        sut.PayrollBonus = "100";
        sut.PayrollDeduction = "50";

        var exception = Record.Exception(() => sut.GeneratePayrollCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.Empty(sut.PayrollSummaries);
        Assert.Equal("100", sut.PayrollBonus);
    }

    [Fact]
    public void CreateEmployeeCommand_Failure_LogsOperationNameOnly_NoPiiOrSalaryLeak()
    {
        var commandService = new StubEmployeeCommandService { CreateEmployeeException = new InvalidOperationException(HrBackendSecret) };
        var logger = new RecordingLogger<HrPageViewModel>();
        var sut = MakeSut(employeeCommandService: commandService, logger: logger);
        sut.NewEmployeeFullName = "New Hire";
        sut.NewEmployeeEmail = "new.hire@rojan.example";

        sut.CreateEmployeeCommand.Execute(null);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("Operation=CreateEmployeeAsync", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains(HrBackendSecret, StringComparison.Ordinal));
        Assert.DoesNotContain(HrBackendSecret, sut.ActionErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratePayrollCommand_Failure_LogsOperationNameOnly_NoSalaryLeak()
    {
        var commandService = new StubPayrollCommandService { GeneratePayrollException = new InvalidOperationException(HrBackendSecret) };
        var logger = new RecordingLogger<HrPageViewModel>();
        var sut = MakeSut(payrollCommandService: commandService, logger: logger);
        sut.PayrollSelectedEmployee = MakeEmployee("employee-1", "Jordan Lee");

        sut.GeneratePayrollCommand.Execute(null);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("Operation=GeneratePayrollAsync", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains(HrBackendSecret, StringComparison.Ordinal));
    }

    [Fact]
    public void CreateShiftCommand_SuccessAfterFailure_ClearsActionError()
    {
        var commandService = new StubShiftCommandService { CreateShiftException = new InvalidOperationException("boom") };
        var sut = MakeSut(shiftCommandService: commandService);
        sut.NewShiftLabel = "Morning - Hair";
        sut.NewShiftStartTime = "09:00";
        sut.NewShiftEndTime = "17:00";
        sut.CreateShiftCommand.Execute(null);
        Assert.True(sut.HasActionError);

        commandService.CreateShiftException = null;
        sut.NewShiftLabel = "Evening - Hair";
        sut.NewShiftStartTime = "13:00";
        sut.NewShiftEndTime = "21:00";
        sut.CreateShiftCommand.Execute(null);

        Assert.False(sut.HasActionError);
        Assert.Null(sut.ActionErrorMessage);
        Assert.Single(sut.Shifts);
    }

    [Fact]
    public void GeneratePayrollCommand_SuccessAfterFailure_ClearsActionError()
    {
        var commandService = new StubPayrollCommandService { GeneratePayrollException = new InvalidOperationException("boom") };
        var sut = MakeSut(payrollCommandService: commandService);
        sut.PayrollSelectedEmployee = MakeEmployee("employee-1", "Jordan Lee");
        sut.GeneratePayrollCommand.Execute(null);
        Assert.True(sut.HasActionError);

        commandService.GeneratePayrollException = null;
        sut.GeneratePayrollCommand.Execute(null);

        Assert.False(sut.HasActionError);
        Assert.Null(sut.ActionErrorMessage);
        Assert.Single(sut.PayrollSummaries);
    }
}
