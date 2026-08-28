using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.HR;
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
        RecordingLogger<HrPageViewModel>? logger = null) => new(
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
        logger);

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
}
