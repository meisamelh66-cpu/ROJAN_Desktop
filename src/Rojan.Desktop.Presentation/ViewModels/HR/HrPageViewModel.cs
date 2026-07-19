using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.HR;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.HR;

/// <summary>
/// Drives HrPage - the HR Dashboard's KPI cards (always visible) plus six
/// switchable sections (Employees/Attendance/Shifts/Leave/Commission/
/// Payroll), each with its own list and a minimal quick-add form, same
/// "master list + quick-add form" shape every other module's page uses.
/// The Employees section additionally shows the selected employee's
/// <see cref="EmployeeProfileViewModel"/> master-detail, same pattern as
/// every other module. Depends only on Application services - never
/// reaches past Application into Domain/Infrastructure.
/// </summary>
public sealed class HrPageViewModel : ViewModelBase
{
    private readonly IEmployeeQueryService _employeeQueryService;
    private readonly IEmployeeCommandService _employeeCommandService;
    private readonly IAttendanceQueryService _attendanceQueryService;
    private readonly IAttendanceCommandService _attendanceCommandService;
    private readonly IShiftQueryService _shiftQueryService;
    private readonly IShiftCommandService _shiftCommandService;
    private readonly ICommissionQueryService _commissionQueryService;
    private readonly ICommissionCommandService _commissionCommandService;
    private readonly IPayrollQueryService _payrollQueryService;
    private readonly IPayrollCommandService _payrollCommandService;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private HrDashboardSummaryDto? _summary;
    private HrSection _selectedSection = HrSection.Employees;
    private string _statusMessage = string.Empty;

    private string _searchText = string.Empty;
    private EmployeeDto? _selectedEmployee;
    private EmployeeProfileViewModel? _profile;

    private string _newEmployeeFullName = string.Empty;
    private string _newEmployeeEmail = string.Empty;
    private string _newEmployeePhone = string.Empty;
    private EmployeeRole _newEmployeeRole = EmployeeRole.Stylist;
    private Department _newEmployeeDepartment = Department.Hair;
    private EmploymentType _newEmployeeEmploymentType = EmploymentType.FullTime;
    private string _newEmployeeBaseSalary = string.Empty;

    private EmployeeDto? _attendanceSelectedEmployee;
    private string _attendanceCheckInTime = string.Empty;
    private string _attendanceNotes = string.Empty;

    private string _newShiftLabel = string.Empty;
    private Department _newShiftDepartment = Department.Hair;
    private string _newShiftStartTime = string.Empty;
    private string _newShiftEndTime = string.Empty;
    private ShiftDto? _assignShiftSelectedShift;
    private EmployeeDto? _assignShiftSelectedEmployee;
    private DateTime _assignShiftDate = DateTime.Today;

    private EmployeeDto? _leaveSelectedEmployee;
    private DateTime _leaveStartDate = DateTime.Today;
    private DateTime _leaveEndDate = DateTime.Today.AddDays(1);
    private string _leaveReason = string.Empty;

    private EmployeeDto? _newRuleSelectedEmployee;
    private CommissionType _newRuleType = CommissionType.Percentage;
    private string _newRuleValue = string.Empty;
    private string _newRuleDescription = string.Empty;

    private EmployeeDto? _payrollSelectedEmployee;
    private int _payrollMonth = DateTime.Today.Month;
    private int _payrollYear = DateTime.Today.Year;
    private string _payrollBonus = string.Empty;
    private string _payrollDeduction = string.Empty;

    public HrPageViewModel(
        IEmployeeQueryService employeeQueryService,
        IEmployeeCommandService employeeCommandService,
        IAttendanceQueryService attendanceQueryService,
        IAttendanceCommandService attendanceCommandService,
        IShiftQueryService shiftQueryService,
        IShiftCommandService shiftCommandService,
        ICommissionQueryService commissionQueryService,
        ICommissionCommandService commissionCommandService,
        IPayrollQueryService payrollQueryService,
        IPayrollCommandService payrollCommandService)
    {
        _employeeQueryService = employeeQueryService;
        _employeeCommandService = employeeCommandService;
        _attendanceQueryService = attendanceQueryService;
        _attendanceCommandService = attendanceCommandService;
        _shiftQueryService = shiftQueryService;
        _shiftCommandService = shiftCommandService;
        _commissionQueryService = commissionQueryService;
        _commissionCommandService = commissionCommandService;
        _payrollQueryService = payrollQueryService;
        _payrollCommandService = payrollCommandService;

        Employees = new ObservableCollection<EmployeeDto>();
        TodayAttendance = new ObservableCollection<AttendanceDto>();
        Shifts = new ObservableCollection<ShiftDto>();
        ShiftAssignments = new ObservableCollection<ShiftAssignmentDto>();
        LeaveRequests = new ObservableCollection<LeaveRequestDto>();
        CommissionRules = new ObservableCollection<CommissionRuleDto>();
        CommissionTransactions = new ObservableCollection<CommissionTransactionDto>();
        PayrollSummaries = new ObservableCollection<PayrollSummaryDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        SelectSectionCommand = new RelayCommand(section => SelectedSection = (HrSection)section!);

        CreateEmployeeCommand = new AsyncRelayCommand(
            _ => CreateEmployeeAsync(),
            _ => !string.IsNullOrWhiteSpace(NewEmployeeFullName) && !string.IsNullOrWhiteSpace(NewEmployeeEmail));

        RecordAttendanceCommand = new AsyncRelayCommand(_ => RecordAttendanceAsync(), _ => AttendanceSelectedEmployee is not null);

        CreateShiftCommand = new AsyncRelayCommand(
            _ => CreateShiftAsync(),
            _ => !string.IsNullOrWhiteSpace(NewShiftLabel) && !string.IsNullOrWhiteSpace(NewShiftStartTime) && !string.IsNullOrWhiteSpace(NewShiftEndTime));
        AssignShiftCommand = new AsyncRelayCommand(
            _ => AssignShiftAsync(),
            _ => AssignShiftSelectedShift is not null && AssignShiftSelectedEmployee is not null);

        RequestLeaveCommand = new AsyncRelayCommand(
            _ => RequestLeaveAsync(),
            _ => LeaveSelectedEmployee is not null && !string.IsNullOrWhiteSpace(LeaveReason));
        ApproveLeaveCommand = new AsyncRelayCommand(parameter => ApproveLeaveAsync(parameter as LeaveRequestDto));
        RejectLeaveCommand = new AsyncRelayCommand(parameter => RejectLeaveAsync(parameter as LeaveRequestDto));

        CreateCommissionRuleCommand = new AsyncRelayCommand(
            _ => CreateCommissionRuleAsync(),
            _ => NewRuleSelectedEmployee is not null && !string.IsNullOrWhiteSpace(NewRuleValue));
        GenerateCommissionsCommand = new AsyncRelayCommand(_ => GenerateCommissionsAsync());

        GeneratePayrollCommand = new AsyncRelayCommand(_ => GeneratePayrollAsync(), _ => PayrollSelectedEmployee is not null);

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, same reasoning as every
        // other page ViewModel's constructor-time load.
        _ = LoadAsync();
    }

    public ObservableCollection<EmployeeDto> Employees { get; }

    public ObservableCollection<AttendanceDto> TodayAttendance { get; }

    public ObservableCollection<ShiftDto> Shifts { get; }

    public ObservableCollection<ShiftAssignmentDto> ShiftAssignments { get; }

    public ObservableCollection<LeaveRequestDto> LeaveRequests { get; }

    public ObservableCollection<CommissionRuleDto> CommissionRules { get; }

    public ObservableCollection<CommissionTransactionDto> CommissionTransactions { get; }

    public ObservableCollection<PayrollSummaryDto> PayrollSummaries { get; }

    public IReadOnlyList<EmployeeRole> AvailableRoles { get; } = Enum.GetValues<EmployeeRole>();

    public IReadOnlyList<Department> AvailableDepartments { get; } = Enum.GetValues<Department>();

    public IReadOnlyList<EmploymentType> AvailableEmploymentTypes { get; } = Enum.GetValues<EmploymentType>();

    public IReadOnlyList<CommissionType> AvailableCommissionTypes { get; } = Enum.GetValues<CommissionType>();

    public ICommand LoadCommand { get; }

    public ICommand SelectSectionCommand { get; }

    public ICommand CreateEmployeeCommand { get; }

    public ICommand RecordAttendanceCommand { get; }

    public ICommand CreateShiftCommand { get; }

    public ICommand AssignShiftCommand { get; }

    public ICommand RequestLeaveCommand { get; }

    public ICommand ApproveLeaveCommand { get; }

    public ICommand RejectLeaveCommand { get; }

    public ICommand CreateCommissionRuleCommand { get; }

    public ICommand GenerateCommissionsCommand { get; }

    public ICommand GeneratePayrollCommand { get; }

    public DashboardState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public HrDashboardSummaryDto? Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public HrSection SelectedSection
    {
        get => _selectedSection;
        set => SetProperty(ref _selectedSection, value);
    }

    /// <summary>Transient feedback for actions without their own dedicated display (e.g. "Generated 2 commissions.").</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = SearchAsync(value);
            }
        }
    }

    public EmployeeDto? SelectedEmployee
    {
        get => _selectedEmployee;
        set
        {
            if (SetProperty(ref _selectedEmployee, value))
            {
                Profile = value is null
                    ? null
                    : new EmployeeProfileViewModel(value.Id, _employeeQueryService, _employeeCommandService, () => _ = LoadAsync());
            }
        }
    }

    public EmployeeProfileViewModel? Profile
    {
        get => _profile;
        private set => SetProperty(ref _profile, value);
    }

    public string NewEmployeeFullName { get => _newEmployeeFullName; set => SetProperty(ref _newEmployeeFullName, value); }

    public string NewEmployeeEmail { get => _newEmployeeEmail; set => SetProperty(ref _newEmployeeEmail, value); }

    public string NewEmployeePhone { get => _newEmployeePhone; set => SetProperty(ref _newEmployeePhone, value); }

    public EmployeeRole NewEmployeeRole { get => _newEmployeeRole; set => SetProperty(ref _newEmployeeRole, value); }

    public Department NewEmployeeDepartment { get => _newEmployeeDepartment; set => SetProperty(ref _newEmployeeDepartment, value); }

    public EmploymentType NewEmployeeEmploymentType { get => _newEmployeeEmploymentType; set => SetProperty(ref _newEmployeeEmploymentType, value); }

    public string NewEmployeeBaseSalary { get => _newEmployeeBaseSalary; set => SetProperty(ref _newEmployeeBaseSalary, value); }

    public EmployeeDto? AttendanceSelectedEmployee { get => _attendanceSelectedEmployee; set => SetProperty(ref _attendanceSelectedEmployee, value); }

    public string AttendanceCheckInTime { get => _attendanceCheckInTime; set => SetProperty(ref _attendanceCheckInTime, value); }

    public string AttendanceNotes { get => _attendanceNotes; set => SetProperty(ref _attendanceNotes, value); }

    public string NewShiftLabel { get => _newShiftLabel; set => SetProperty(ref _newShiftLabel, value); }

    public Department NewShiftDepartment { get => _newShiftDepartment; set => SetProperty(ref _newShiftDepartment, value); }

    public string NewShiftStartTime { get => _newShiftStartTime; set => SetProperty(ref _newShiftStartTime, value); }

    public string NewShiftEndTime { get => _newShiftEndTime; set => SetProperty(ref _newShiftEndTime, value); }

    public ShiftDto? AssignShiftSelectedShift { get => _assignShiftSelectedShift; set => SetProperty(ref _assignShiftSelectedShift, value); }

    public EmployeeDto? AssignShiftSelectedEmployee { get => _assignShiftSelectedEmployee; set => SetProperty(ref _assignShiftSelectedEmployee, value); }

    public DateTime AssignShiftDate { get => _assignShiftDate; set => SetProperty(ref _assignShiftDate, value); }

    public EmployeeDto? LeaveSelectedEmployee { get => _leaveSelectedEmployee; set => SetProperty(ref _leaveSelectedEmployee, value); }

    public DateTime LeaveStartDate { get => _leaveStartDate; set => SetProperty(ref _leaveStartDate, value); }

    public DateTime LeaveEndDate { get => _leaveEndDate; set => SetProperty(ref _leaveEndDate, value); }

    public string LeaveReason { get => _leaveReason; set => SetProperty(ref _leaveReason, value); }

    public EmployeeDto? NewRuleSelectedEmployee { get => _newRuleSelectedEmployee; set => SetProperty(ref _newRuleSelectedEmployee, value); }

    public CommissionType NewRuleType { get => _newRuleType; set => SetProperty(ref _newRuleType, value); }

    public string NewRuleValue { get => _newRuleValue; set => SetProperty(ref _newRuleValue, value); }

    public string NewRuleDescription { get => _newRuleDescription; set => SetProperty(ref _newRuleDescription, value); }

    public EmployeeDto? PayrollSelectedEmployee { get => _payrollSelectedEmployee; set => SetProperty(ref _payrollSelectedEmployee, value); }

    public int PayrollMonth { get => _payrollMonth; set => SetProperty(ref _payrollMonth, value); }

    public int PayrollYear { get => _payrollYear; set => SetProperty(ref _payrollYear, value); }

    public string PayrollBonus { get => _payrollBonus; set => SetProperty(ref _payrollBonus, value); }

    public string PayrollDeduction { get => _payrollDeduction; set => SetProperty(ref _payrollDeduction, value); }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var employees = await _employeeQueryService.GetEmployeesAsync().ConfigureAwait(true);
            ReplaceEmployees(employees);

            Summary = await _employeeQueryService.GetDashboardSummaryAsync().ConfigureAwait(true);

            var todayAttendance = await _attendanceQueryService.GetTodayAttendanceAsync().ConfigureAwait(true);
            TodayAttendance.Clear();
            foreach (var attendance in todayAttendance)
            {
                TodayAttendance.Add(attendance);
            }

            var shifts = await _shiftQueryService.GetShiftsAsync().ConfigureAwait(true);
            Shifts.Clear();
            foreach (var shift in shifts)
            {
                Shifts.Add(shift);
            }

            var assignments = await _shiftQueryService.GetAllShiftAssignmentsAsync().ConfigureAwait(true);
            ShiftAssignments.Clear();
            foreach (var assignment in assignments)
            {
                ShiftAssignments.Add(assignment);
            }

            var leaveRequests = await _attendanceQueryService.GetLeaveRequestsAsync().ConfigureAwait(true);
            LeaveRequests.Clear();
            foreach (var leaveRequest in leaveRequests)
            {
                LeaveRequests.Add(leaveRequest);
            }

            var commissionRules = await _commissionQueryService.GetCommissionRulesAsync().ConfigureAwait(true);
            CommissionRules.Clear();
            foreach (var rule in commissionRules)
            {
                CommissionRules.Add(rule);
            }

            var commissionTransactions = await _commissionQueryService.GetAllCommissionTransactionsAsync().ConfigureAwait(true);
            CommissionTransactions.Clear();
            foreach (var transaction in commissionTransactions)
            {
                CommissionTransactions.Add(transaction);
            }

            var payrollSummaries = await _payrollQueryService.GetPayrollSummariesAsync().ConfigureAwait(true);
            PayrollSummaries.Clear();
            foreach (var summary in payrollSummaries)
            {
                PayrollSummaries.Add(summary);
            }

            State = employees.Count == 0 ? DashboardState.Empty : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
        }
    }

    private async Task SearchAsync(string searchText)
    {
        try
        {
            var results = await _employeeQueryService.SearchEmployeesAsync(searchText).ConfigureAwait(true);
            if (!string.Equals(searchText, SearchText, StringComparison.Ordinal))
            {
                return;
            }

            ReplaceEmployees(results);
        }
#pragma warning disable CA1031 // Same top-level boundary reasoning as LoadAsync - a failed search must surface as the Error state, not crash the page.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            if (string.Equals(searchText, SearchText, StringComparison.Ordinal))
            {
                ErrorMessage = exception.Message;
                State = DashboardState.Error;
            }
        }
    }

    private void ReplaceEmployees(IReadOnlyList<EmployeeDto> employees)
    {
        Employees.Clear();
        foreach (var employee in employees)
        {
            Employees.Add(employee);
        }

        if (SelectedEmployee is null || !Employees.Contains(SelectedEmployee))
        {
            SelectedEmployee = Employees.Count > 0 ? Employees[0] : null;
        }
    }

    private async Task CreateEmployeeAsync()
    {
        _ = decimal.TryParse(NewEmployeeBaseSalary, out var baseSalary);
        var request = new CreateEmployeeRequest(
            string.Empty, NewEmployeeFullName, NewEmployeeEmail, NewEmployeePhone,
            NewEmployeeRole, NewEmployeeDepartment, NewEmployeeEmploymentType, DateOnly.FromDateTime(DateTime.Today), baseSalary);

        var created = await _employeeCommandService.CreateEmployeeAsync(request).ConfigureAwait(true);

        NewEmployeeFullName = string.Empty;
        NewEmployeeEmail = string.Empty;
        NewEmployeePhone = string.Empty;
        NewEmployeeBaseSalary = string.Empty;

        await LoadAsync().ConfigureAwait(true);
        SelectedEmployee = Employees.FirstOrDefault(employee => employee.Id == created.Id);
    }

    private async Task RecordAttendanceAsync()
    {
        if (AttendanceSelectedEmployee is null)
        {
            return;
        }

        TimeSpan? checkInTime = TimeSpan.TryParse(AttendanceCheckInTime, out var parsed) ? parsed : null;

        var request = new RecordAttendanceRequest(
            AttendanceSelectedEmployee.Id, DateOnly.FromDateTime(DateTime.Today), checkInTime, null, null, AttendanceNotes);

        await _attendanceCommandService.RecordAttendanceAsync(request).ConfigureAwait(true);

        AttendanceCheckInTime = string.Empty;
        AttendanceNotes = string.Empty;

        var todayAttendance = await _attendanceQueryService.GetTodayAttendanceAsync().ConfigureAwait(true);
        TodayAttendance.Clear();
        foreach (var attendance in todayAttendance)
        {
            TodayAttendance.Add(attendance);
        }

        Summary = await _employeeQueryService.GetDashboardSummaryAsync().ConfigureAwait(true);
    }

    private async Task CreateShiftAsync()
    {
        if (!TimeSpan.TryParse(NewShiftStartTime, out var start) || !TimeSpan.TryParse(NewShiftEndTime, out var end))
        {
            return;
        }

        var request = new CreateShiftRequest(NewShiftLabel, NewShiftDepartment, start, end);
        var created = await _shiftCommandService.CreateShiftAsync(request).ConfigureAwait(true);

        NewShiftLabel = string.Empty;
        NewShiftStartTime = string.Empty;
        NewShiftEndTime = string.Empty;

        Shifts.Add(created);
    }

    private async Task AssignShiftAsync()
    {
        if (AssignShiftSelectedShift is null || AssignShiftSelectedEmployee is null)
        {
            return;
        }

        var request = new AssignShiftRequest(AssignShiftSelectedShift.Id, AssignShiftSelectedEmployee.Id, DateOnly.FromDateTime(AssignShiftDate));
        var created = await _shiftCommandService.AssignShiftAsync(request).ConfigureAwait(true);

        ShiftAssignments.Add(created);
    }

    private async Task RequestLeaveAsync()
    {
        if (LeaveSelectedEmployee is null)
        {
            return;
        }

        var request = new CreateLeaveRequestRequest(
            LeaveSelectedEmployee.Id, DateOnly.FromDateTime(LeaveStartDate), DateOnly.FromDateTime(LeaveEndDate), LeaveReason);
        var created = await _attendanceCommandService.RequestLeaveAsync(request).ConfigureAwait(true);

        LeaveReason = string.Empty;
        LeaveRequests.Insert(0, created);
    }

    private async Task ApproveLeaveAsync(LeaveRequestDto? leaveRequest)
    {
        if (leaveRequest is null)
        {
            return;
        }

        var updated = await _attendanceCommandService.ApproveLeaveAsync(leaveRequest.Id).ConfigureAwait(true);
        ReplaceLeaveRequest(updated);
    }

    private async Task RejectLeaveAsync(LeaveRequestDto? leaveRequest)
    {
        if (leaveRequest is null)
        {
            return;
        }

        var updated = await _attendanceCommandService.RejectLeaveAsync(leaveRequest.Id).ConfigureAwait(true);
        ReplaceLeaveRequest(updated);
    }

    private void ReplaceLeaveRequest(LeaveRequestDto updated)
    {
        var index = LeaveRequests.ToList().FindIndex(l => l.Id == updated.Id);
        if (index >= 0)
        {
            LeaveRequests[index] = updated;
        }
    }

    private async Task CreateCommissionRuleAsync()
    {
        if (NewRuleSelectedEmployee is null || !decimal.TryParse(NewRuleValue, out var value))
        {
            return;
        }

        var request = new CreateCommissionRuleRequest(NewRuleSelectedEmployee.Id, NewRuleType, value, NewRuleDescription);
        var created = await _commissionCommandService.CreateCommissionRuleAsync(request).ConfigureAwait(true);

        NewRuleValue = string.Empty;
        NewRuleDescription = string.Empty;
        CommissionRules.Add(created);
    }

    private async Task GenerateCommissionsAsync()
    {
        var generated = await _commissionCommandService.GenerateCommissionsFromAccountingAsync().ConfigureAwait(true);
        foreach (var transaction in generated)
        {
            CommissionTransactions.Insert(0, transaction);
        }

        StatusMessage = generated.Count == 1
            ? "Generated 1 new commission from Accounting."
            : $"Generated {generated.Count} new commissions from Accounting.";
    }

    private async Task GeneratePayrollAsync()
    {
        if (PayrollSelectedEmployee is null)
        {
            return;
        }

        _ = decimal.TryParse(PayrollBonus, out var bonus);
        _ = decimal.TryParse(PayrollDeduction, out var deduction);

        var request = new GeneratePayrollRequest(PayrollSelectedEmployee.Id, PayrollMonth, PayrollYear, bonus, deduction);
        var created = await _payrollCommandService.GeneratePayrollSummaryAsync(request).ConfigureAwait(true);

        PayrollBonus = string.Empty;
        PayrollDeduction = string.Empty;
        PayrollSummaries.Insert(0, created);
    }
}
