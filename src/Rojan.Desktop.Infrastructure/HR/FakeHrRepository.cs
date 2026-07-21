using Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Infrastructure.HR;

/// <summary>
/// In-memory <see cref="IHrRepository"/> providing static sample data -
/// Phase 19 explicitly has no backend integration yet, same as every
/// other vertical slice in this app. Instance (not static) mutable
/// state, same reasoning as <c>Customers.FakeCustomerRepository</c>: this
/// fake has real create/status-transition/generation commands, so it
/// needs to remember writes for the app's lifetime - registered as a DI
/// singleton (see Infrastructure's ServiceCollectionExtensions). Five of
/// the twenty seeded employees cross-reference the real specialist ids
/// already seeded in <c>Specialists.FakeSpecialistRepository</c>
/// ("specialist-1".."specialist-5") for a cohesive demo - not a real
/// cross-slice link, just consistent naming, same reasoning as every
/// other cross-slice reference in this app. Commission history is
/// deliberately seeded for only four of the five real Accounting
/// invoices that have a paid/partially-paid status and a booking behind
/// them ("invoice-1", "invoice-2", "invoice-3", "invoice-5") - "invoice-8"
/// is left unprocessed so <c>CommissionCommandService.GenerateCommissionsFromAccountingAsync</c>
/// has something real to generate live. The small artificial delays are
/// deliberate, same reasoning as every other fake repository: without
/// them, Loading states would never actually be observable when running
/// the app.
/// </summary>
public sealed class FakeHrRepository : IHrRepository
{
    private readonly List<Employee> _employees;
    private readonly List<EmployeeProfile> _employeeProfiles;
    private readonly List<Shift> _shifts;
    private readonly List<ShiftAssignment> _shiftAssignments;
    private readonly List<Attendance> _attendance;
    private readonly List<LeaveRequest> _leaveRequests;
    private readonly List<CommissionRule> _commissionRules;
    private readonly List<CommissionTransaction> _commissionTransactions;
    private readonly List<PayrollSummary> _payrollSummaries;

    public FakeHrRepository()
    {
        var now = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(now.Date);
        var yesterday = today.AddDays(-1);

        _employees =
        [
            new Employee("employee-1", "specialist-1", "کیانا رادمنش", "kiana.radmanesh@rojan.example", "0912-300-2001", EmployeeRole.Colorist, Department.Hair, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2021, 3, 10), 32000000m),
            new Employee("employee-2", "specialist-2", "سارا امینی", "sara.amini@rojan.example", "0912-300-2002", EmployeeRole.Stylist, Department.Hair, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2022, 6, 1), 29000000m),
            new Employee("employee-3", "specialist-3", "مهسا کریمی", "mahsa.karimi@rojan.example", "0912-300-2003", EmployeeRole.MassageTherapist, Department.Massage, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2020, 11, 15), 28000000m),
            new Employee("employee-4", "specialist-4", "نیلوفر صفایی", "niloofar.safaei@rojan.example", "0912-300-2004", EmployeeRole.Stylist, Department.Hair, EmploymentType.PartTime, EmployeeStatus.Active, new DateOnly(2023, 9, 4), 20000000m),
            new Employee("employee-5", "specialist-5", "پویا احمدپور", "pouya.ahmadpour@rojan.example", "0912-300-2005", EmployeeRole.Colorist, Department.Hair, EmploymentType.FullTime, EmployeeStatus.Inactive, new DateOnly(2019, 5, 20), 26000000m),
            new Employee("employee-6", string.Empty, "مریم رحیمی", "maryam.rahimi@rojan.example", "0912-300-2006", EmployeeRole.Receptionist, Department.Reception, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2022, 1, 10), 22000000m),
            new Employee("employee-7", string.Empty, "آرمان قاسمی", "arman.ghasemi@rojan.example", "0912-300-2007", EmployeeRole.Receptionist, Department.Reception, EmploymentType.PartTime, EmployeeStatus.Active, new DateOnly(2023, 4, 18), 16000000m),
            new Employee("employee-8", string.Empty, "الهام توکلی", "elham.tavakoli@rojan.example", "0912-300-2008", EmployeeRole.Manager, Department.Management, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2018, 8, 1), 42000000m),
            new Employee("employee-9", string.Empty, "امیرحسین یوسفی", "amirhossein.yousefi@rojan.example", "0912-300-2009", EmployeeRole.NailTechnician, Department.Nails, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2022, 10, 3), 21000000m),
            new Employee("employee-10", string.Empty, "ترانه محمودی", "taraneh.mahmoudi@rojan.example", "0912-300-2010", EmployeeRole.NailTechnician, Department.Nails, EmploymentType.PartTime, EmployeeStatus.Active, new DateOnly(2023, 2, 14), 18000000m),
            new Employee("employee-11", string.Empty, "سامان فرهادی", "saman.farhadi@rojan.example", "0912-300-2011", EmployeeRole.Esthetician, Department.SkinCare, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2021, 7, 22), 24000000m),
            new Employee("employee-12", string.Empty, "یاسمن نجفی", "yasaman.najafi@rojan.example", "0912-300-2012", EmployeeRole.Esthetician, Department.SkinCare, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2020, 12, 5), 25000000m),
            new Employee("employee-13", string.Empty, "بابک شریفی", "babak.sharifi@rojan.example", "0912-300-2013", EmployeeRole.MassageTherapist, Department.Massage, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2019, 9, 30), 27000000m),
            new Employee("employee-14", string.Empty, "رومینا اصغری", "romina.asghari@rojan.example", "0912-300-2014", EmployeeRole.Stylist, Department.Hair, EmploymentType.FullTime, EmployeeStatus.Suspended, new DateOnly(2022, 3, 8), 26000000m),
            new Employee("employee-15", string.Empty, "هستی جعفری", "hasti.jafari@rojan.example", "0912-300-2015", EmployeeRole.Colorist, Department.Hair, EmploymentType.Contractor, EmployeeStatus.Active, new DateOnly(2023, 11, 1), 23000000m),
            new Employee("employee-16", string.Empty, "کاوه مرادی", "kaveh.moradi@rojan.example", "0912-300-2016", EmployeeRole.Manager, Department.Management, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2017, 4, 12), 40000000m),
            new Employee("employee-17", string.Empty, "پرنیا حیدری", "parnia.heidari@rojan.example", "0912-300-2017", EmployeeRole.Receptionist, Department.Reception, EmploymentType.FullTime, EmployeeStatus.OnLeave, new DateOnly(2021, 1, 25), 21000000m),
            new Employee("employee-18", string.Empty, "رامین قربانی", "ramin.ghorbani@rojan.example", "0912-300-2018", EmployeeRole.NailTechnician, Department.Nails, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2022, 8, 19), 20000000m),
            new Employee("employee-19", string.Empty, "دنیا عزیزی", "donya.azizi@rojan.example", "0912-300-2019", EmployeeRole.MassageTherapist, Department.Massage, EmploymentType.PartTime, EmployeeStatus.Active, new DateOnly(2023, 5, 7), 19000000m),
            new Employee("employee-20", string.Empty, "فرهاد نوروزی", "farhad.nowrouzi@rojan.example", "0912-300-2020", EmployeeRole.Esthetician, Department.SkinCare, EmploymentType.Contractor, EmployeeStatus.Active, new DateOnly(2023, 7, 15), 22000000m),
        ];

        _employeeProfiles =
        [
            new EmployeeProfile("profile-1", "employee-1", "متخصص ارشد رنگ مو با بیش از ۸ سال سابقه در بالیاژ و اصلاح رنگ.", "بالیاژ، اصلاح رنگ، فویلینگ", "شیوا رادمنش", "0912-400-1001"),
            new EmployeeProfile("profile-2", "employee-2", "استایلیست و مشاور مشتری، مسلط بر کوتاهی‌های مدرن.", "کوتاهی دقیق، مشاوره", "وحید امینی", "0912-400-1002"),
            new EmployeeProfile("profile-3", "employee-3", "درمانگر مجاز ماساژ، متخصص بافت عمقی و سنگ داغ.", "بافت عمقی، سنگ داغ، رایحه‌درمانی", "لیلا کریمی", "0912-400-1003"),
            new EmployeeProfile("profile-6", "employee-6", "مسئول پذیرش، انجام امور نوبت‌دهی و پذیرش مشتریان.", "نوبت‌دهی، صندوق فروش، ارتباط با مشتری", "کامران رحیمی", "0912-400-1006"),
            new EmployeeProfile("profile-8", "employee-8", "مدیر عملیات سالن، مسئول نظارت بر نیروی انسانی و روابط با تأمین‌کنندگان.", "عملیات، نیروی انسانی، مدیریت تأمین‌کننده", "حسین توکلی", "0912-400-1008"),
        ];

        _shifts =
        [
            new Shift("shift-1", "شیفت صبح - مو", Department.Hair, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
            new Shift("shift-2", "شیفت صبح - ناخن", Department.Nails, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
            new Shift("shift-3", "شیفت صبح - ماساژ", Department.Massage, new TimeSpan(10, 0, 0), new TimeSpan(18, 0, 0)),
            new Shift("shift-4", "شیفت صبح - پذیرش", Department.Reception, new TimeSpan(8, 30, 0), new TimeSpan(16, 30, 0)),
            new Shift("shift-5", "شیفت روز - پوست", Department.SkinCare, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
            new Shift("shift-6", "شیفت روز - مدیریت", Department.Management, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
        ];

        _shiftAssignments =
        [
            new ShiftAssignment("assignment-1", "shift-1", "employee-1", "کیانا رادمنش", today),
            new ShiftAssignment("assignment-2", "shift-1", "employee-2", "سارا امینی", today),
            new ShiftAssignment("assignment-3", "shift-3", "employee-3", "مهسا کریمی", today),
            new ShiftAssignment("assignment-4", "shift-1", "employee-4", "نیلوفر صفایی", today),
            new ShiftAssignment("assignment-5", "shift-4", "employee-6", "مریم رحیمی", today),
            new ShiftAssignment("assignment-6", "shift-2", "employee-9", "امیرحسین یوسفی", today),
            new ShiftAssignment("assignment-7", "shift-1", "employee-1", "کیانا رادمنش", yesterday),
            new ShiftAssignment("assignment-8", "shift-1", "employee-14", "رومینا اصغری", yesterday),
            new ShiftAssignment("assignment-9", "shift-1", "employee-2", "سارا امینی", today.AddDays(1)),
            new ShiftAssignment("assignment-10", "shift-3", "employee-3", "مهسا کریمی", today.AddDays(1)),
        ];

        _attendance =
        [
            new Attendance("attendance-1", "employee-1", "کیانا رادمنش", today, new TimeSpan(9, 5, 0), null, AttendanceStatus.Present, string.Empty),
            new Attendance("attendance-2", "employee-2", "سارا امینی", today, new TimeSpan(9, 20, 0), null, AttendanceStatus.Late, string.Empty),
            new Attendance("attendance-3", "employee-3", "مهسا کریمی", today, new TimeSpan(10, 0, 0), null, AttendanceStatus.Present, string.Empty),
            new Attendance("attendance-4", "employee-4", "نیلوفر صفایی", today, null, null, AttendanceStatus.Absent, "تماس گرفت و اعلام بیماری کرد."),
            new Attendance("attendance-5", "employee-6", "مریم رحیمی", today, new TimeSpan(8, 35, 0), null, AttendanceStatus.Present, string.Empty),
            new Attendance("attendance-6", "employee-9", "امیرحسین یوسفی", today, new TimeSpan(9, 15, 0), null, AttendanceStatus.Late, string.Empty),
            new Attendance("attendance-7", "employee-17", "پرنیا حیدری", today, null, null, AttendanceStatus.Vacation, "مرخصی تأییدشده."),
            new Attendance("attendance-8", "employee-1", "کیانا رادمنش", yesterday, new TimeSpan(8, 58, 0), new TimeSpan(17, 5, 0), AttendanceStatus.Present, string.Empty),
            new Attendance("attendance-9", "employee-14", "رومینا اصغری", yesterday, new TimeSpan(9, 30, 0), new TimeSpan(17, 0, 0), AttendanceStatus.Late, string.Empty),
        ];

        _leaveRequests =
        [
            new LeaveRequest("leave-1", "employee-17", "پرنیا حیدری", today.AddDays(-2), today.AddDays(3), "مسائل خانوادگی فوری.", LeaveStatus.Approved, now.AddDays(-5)),
            new LeaveRequest("leave-2", "employee-6", "مریم رحیمی", today.AddDays(10), today.AddDays(14), "مرخصی استحقاقی.", LeaveStatus.Pending, now.AddDays(-1)),
            new LeaveRequest("leave-3", "employee-9", "امیرحسین یوسفی", today.AddDays(-20), today.AddDays(-18), "امور شخصی.", LeaveStatus.Rejected, now.AddDays(-25)),
            new LeaveRequest("leave-4", "employee-2", "سارا امینی", today.AddDays(-30), today.AddDays(-28), "مرخصی استحقاقی.", LeaveStatus.Approved, now.AddDays(-35)),
        ];

        _commissionRules =
        [
            new CommissionRule("rule-1", "employee-1", "کیانا رادمنش", CommissionType.Percentage, 0.15m, "نرخ رنگساز ارشد."),
            new CommissionRule("rule-2", "employee-2", "سارا امینی", CommissionType.Percentage, 0.12m, "نرخ استایلیست."),
            new CommissionRule("rule-3", "employee-3", "مهسا کریمی", CommissionType.Percentage, 0.10m, "نرخ استاندارد درمانگر."),
            new CommissionRule("rule-4", "employee-4", "نیلوفر صفایی", CommissionType.FixedAmount, 150000m, "نرخ ثابت استایلیست جونیور."),
        ];

        _commissionTransactions =
        [
            new CommissionTransaction("commission-1", "employee-1", "کیانا رادمنش", "invoice-3", "کوتاهی و استایل مو", 896400m, 134500m, now.AddDays(-95)),
            new CommissionTransaction("commission-2", "employee-1", "کیانا رادمنش", "invoice-5", "اصلاح رنگ ریشه", 1576800m, 236500m, now.AddDays(-1)),
            new CommissionTransaction("commission-3", "employee-3", "مهسا کریمی", "invoice-1", "مانیکور", 691200m, 69100m, now.AddDays(-5)),
            new CommissionTransaction("commission-4", "employee-3", "مهسا کریمی", "invoice-2", "فیشیال ترمیمی", 1155600m, 115600m, now.AddDays(-10)),
        ];

        var lastMonthDate = now.AddMonths(-1);
        _payrollSummaries =
        [
            new PayrollSummary("payroll-1", "employee-1", "کیانا رادمنش", lastMonthDate.Month, lastMonthDate.Year, 32000000m, 4500000m, 1000000m, 500000m, 37000000m, lastMonthDate),
            new PayrollSummary("payroll-2", "employee-3", "مهسا کریمی", lastMonthDate.Month, lastMonthDate.Year, 28000000m, 3100000m, 0m, 250000m, 30850000m, lastMonthDate),
        ];
    }

    public async Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken).ConfigureAwait(true);
        return _employees.ToList();
    }

    public async Task<Employee?> GetEmployeeByIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _employees.FirstOrDefault(employee => employee.Id == employeeId);
    }

    public async Task<Employee> CreateEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _employees.Add(employee);
        return employee;
    }

    public async Task<Employee> UpdateEmployeeStatusAsync(string employeeId, EmployeeStatus status, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        var index = _employees.FindIndex(employee => employee.Id == employeeId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Employee '{employeeId}' was not found.");
        }

        var updated = _employees[index] with { Status = status };
        _employees[index] = updated;
        return updated;
    }

    public async Task<Employee> UpdateEmployeeDepartmentAsync(string employeeId, Department department, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        var index = _employees.FindIndex(employee => employee.Id == employeeId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Employee '{employeeId}' was not found.");
        }

        var updated = _employees[index] with { Department = department };
        _employees[index] = updated;
        return updated;
    }

    public async Task<EmployeeProfile?> GetEmployeeProfileAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _employeeProfiles.FirstOrDefault(profile => profile.EmployeeId == employeeId);
    }

    public async Task<EmployeeProfile> UpsertEmployeeProfileAsync(EmployeeProfile profile, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        var index = _employeeProfiles.FindIndex(existing => existing.EmployeeId == profile.EmployeeId);
        if (index < 0)
        {
            _employeeProfiles.Add(profile);
        }
        else
        {
            _employeeProfiles[index] = profile;
        }

        return profile;
    }

    public async Task<IReadOnlyList<Shift>> GetShiftsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _shifts.ToList();
    }

    public async Task<Shift> CreateShiftAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _shifts.Add(shift);
        return shift;
    }

    public async Task<IReadOnlyList<ShiftAssignment>> GetShiftAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(300, cancellationToken).ConfigureAwait(true);
        return _shiftAssignments.ToList();
    }

    public async Task<ShiftAssignment> CreateShiftAssignmentAsync(ShiftAssignment assignment, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _shiftAssignments.Add(assignment);
        return assignment;
    }

    public async Task<IReadOnlyList<Attendance>> GetAttendanceAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(300, cancellationToken).ConfigureAwait(true);
        return _attendance.ToList();
    }

    public async Task<Attendance> RecordAttendanceAsync(Attendance attendance, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _attendance.Add(attendance);
        return attendance;
    }

    public async Task<Attendance> UpdateAttendanceAsync(Attendance attendance, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        var index = _attendance.FindIndex(existing => existing.Id == attendance.Id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Attendance record '{attendance.Id}' was not found.");
        }

        _attendance[index] = attendance;
        return attendance;
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetLeaveRequestsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(300, cancellationToken).ConfigureAwait(true);
        return _leaveRequests.ToList();
    }

    public async Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _leaveRequests.Add(leaveRequest);
        return leaveRequest;
    }

    public async Task<LeaveRequest> UpdateLeaveRequestStatusAsync(string leaveRequestId, LeaveStatus status, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        var index = _leaveRequests.FindIndex(existing => existing.Id == leaveRequestId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Leave request '{leaveRequestId}' was not found.");
        }

        var updated = _leaveRequests[index] with { Status = status };
        _leaveRequests[index] = updated;
        return updated;
    }

    public async Task<IReadOnlyList<CommissionRule>> GetCommissionRulesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _commissionRules.ToList();
    }

    public async Task<CommissionRule> CreateCommissionRuleAsync(CommissionRule rule, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _commissionRules.Add(rule);
        return rule;
    }

    public async Task<IReadOnlyList<CommissionTransaction>> GetCommissionTransactionsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(300, cancellationToken).ConfigureAwait(true);
        return _commissionTransactions.ToList();
    }

    public async Task<CommissionTransaction> CreateCommissionTransactionAsync(CommissionTransaction transaction, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _commissionTransactions.Add(transaction);
        return transaction;
    }

    public async Task<IReadOnlyList<PayrollSummary>> GetPayrollSummariesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(300, cancellationToken).ConfigureAwait(true);
        return _payrollSummaries.ToList();
    }

    public async Task<PayrollSummary> CreatePayrollSummaryAsync(PayrollSummary summary, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _payrollSummaries.Add(summary);
        return summary;
    }
}
