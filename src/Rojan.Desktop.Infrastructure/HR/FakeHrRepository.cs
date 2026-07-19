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
            new Employee("employee-1", "specialist-1", "Jordan Lee", "jordan.lee@rojan.example", "+1 (555) 030-2001", EmployeeRole.Colorist, Department.Hair, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2021, 3, 10), 3200m),
            new Employee("employee-2", "specialist-2", "Priya Nair", "priya.nair@rojan.example", "+1 (555) 030-2002", EmployeeRole.Stylist, Department.Hair, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2022, 6, 1), 2900m),
            new Employee("employee-3", "specialist-3", "Casey Morgan", "casey.morgan@rojan.example", "+1 (555) 030-2003", EmployeeRole.MassageTherapist, Department.Massage, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2020, 11, 15), 2800m),
            new Employee("employee-4", "specialist-4", "Riley Chen", "riley.chen@rojan.example", "+1 (555) 030-2004", EmployeeRole.Stylist, Department.Hair, EmploymentType.PartTime, EmployeeStatus.Active, new DateOnly(2023, 9, 4), 2000m),
            new Employee("employee-5", "specialist-5", "Sam Torres", "sam.torres@rojan.example", "+1 (555) 030-2005", EmployeeRole.Colorist, Department.Hair, EmploymentType.FullTime, EmployeeStatus.Inactive, new DateOnly(2019, 5, 20), 2600m),
            new Employee("employee-6", string.Empty, "Amelia Ross", "amelia.ross@rojan.example", "+1 (555) 030-2006", EmployeeRole.Receptionist, Department.Reception, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2022, 1, 10), 2200m),
            new Employee("employee-7", string.Empty, "Noah Whitfield", "noah.whitfield@rojan.example", "+1 (555) 030-2007", EmployeeRole.Receptionist, Department.Reception, EmploymentType.PartTime, EmployeeStatus.Active, new DateOnly(2023, 4, 18), 1600m),
            new Employee("employee-8", string.Empty, "Olivia Bennett", "olivia.bennett@rojan.example", "+1 (555) 030-2008", EmployeeRole.Manager, Department.Management, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2018, 8, 1), 4200m),
            new Employee("employee-9", string.Empty, "Ethan Park", "ethan.park@rojan.example", "+1 (555) 030-2009", EmployeeRole.NailTechnician, Department.Nails, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2022, 10, 3), 2100m),
            new Employee("employee-10", string.Empty, "Grace Sullivan", "grace.sullivan@rojan.example", "+1 (555) 030-2010", EmployeeRole.NailTechnician, Department.Nails, EmploymentType.PartTime, EmployeeStatus.Active, new DateOnly(2023, 2, 14), 1800m),
            new Employee("employee-11", string.Empty, "Liam Chandler", "liam.chandler@rojan.example", "+1 (555) 030-2011", EmployeeRole.Esthetician, Department.SkinCare, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2021, 7, 22), 2400m),
            new Employee("employee-12", string.Empty, "Mia Alvarez", "mia.alvarez@rojan.example", "+1 (555) 030-2012", EmployeeRole.Esthetician, Department.SkinCare, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2020, 12, 5), 2500m),
            new Employee("employee-13", string.Empty, "Noah Kessler", "noah.kessler@rojan.example", "+1 (555) 030-2013", EmployeeRole.MassageTherapist, Department.Massage, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2019, 9, 30), 2700m),
            new Employee("employee-14", string.Empty, "Sophia Delgado", "sophia.delgado@rojan.example", "+1 (555) 030-2014", EmployeeRole.Stylist, Department.Hair, EmploymentType.FullTime, EmployeeStatus.Suspended, new DateOnly(2022, 3, 8), 2600m),
            new Employee("employee-15", string.Empty, "Zoe Martinez", "zoe.martinez@rojan.example", "+1 (555) 030-2015", EmployeeRole.Colorist, Department.Hair, EmploymentType.Contractor, EmployeeStatus.Active, new DateOnly(2023, 11, 1), 2300m),
            new Employee("employee-16", string.Empty, "Lucas Bryant", "lucas.bryant@rojan.example", "+1 (555) 030-2016", EmployeeRole.Manager, Department.Management, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2017, 4, 12), 4000m),
            new Employee("employee-17", string.Empty, "Isabella Cruz", "isabella.cruz@rojan.example", "+1 (555) 030-2017", EmployeeRole.Receptionist, Department.Reception, EmploymentType.FullTime, EmployeeStatus.OnLeave, new DateOnly(2021, 1, 25), 2100m),
            new Employee("employee-18", string.Empty, "Henry Walsh", "henry.walsh@rojan.example", "+1 (555) 030-2018", EmployeeRole.NailTechnician, Department.Nails, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2022, 8, 19), 2000m),
            new Employee("employee-19", string.Empty, "Ava Fitzgerald", "ava.fitzgerald@rojan.example", "+1 (555) 030-2019", EmployeeRole.MassageTherapist, Department.Massage, EmploymentType.PartTime, EmployeeStatus.Active, new DateOnly(2023, 5, 7), 1900m),
            new Employee("employee-20", string.Empty, "Daniel Osei", "daniel.osei@rojan.example", "+1 (555) 030-2020", EmployeeRole.Esthetician, Department.SkinCare, EmploymentType.Contractor, EmployeeStatus.Active, new DateOnly(2023, 7, 15), 2200m),
        ];

        _employeeProfiles =
        [
            new EmployeeProfile("profile-1", "employee-1", "Senior colour specialist with 8+ years in balayage and corrective colour.", "Balayage, Colour Correction, Foiling", "Mei Lee", "+1 (555) 040-1001"),
            new EmployeeProfile("profile-2", "employee-2", "Stylist and client consultant, strong in modern cuts.", "Precision Cutting, Consultation", "Raj Nair", "+1 (555) 040-1002"),
            new EmployeeProfile("profile-3", "employee-3", "Licensed massage therapist specialising in deep tissue and hot stone.", "Deep Tissue, Hot Stone, Aromatherapy", "Dana Morgan", "+1 (555) 040-1003"),
            new EmployeeProfile("profile-6", "employee-6", "Front-desk lead, handles scheduling and client check-in.", "Scheduling, POS, Client Relations", "Ben Ross", "+1 (555) 040-1006"),
            new EmployeeProfile("profile-8", "employee-8", "Salon operations manager overseeing daily staffing and vendor relations.", "Operations, Staffing, Vendor Management", "Tom Bennett", "+1 (555) 040-1008"),
        ];

        _shifts =
        [
            new Shift("shift-1", "Morning - Hair", Department.Hair, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
            new Shift("shift-2", "Morning - Nails", Department.Nails, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
            new Shift("shift-3", "Morning - Massage", Department.Massage, new TimeSpan(10, 0, 0), new TimeSpan(18, 0, 0)),
            new Shift("shift-4", "Reception Morning", Department.Reception, new TimeSpan(8, 30, 0), new TimeSpan(16, 30, 0)),
            new Shift("shift-5", "SkinCare Day", Department.SkinCare, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
            new Shift("shift-6", "Management Day", Department.Management, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
        ];

        _shiftAssignments =
        [
            new ShiftAssignment("assignment-1", "shift-1", "employee-1", "Jordan Lee", today),
            new ShiftAssignment("assignment-2", "shift-1", "employee-2", "Priya Nair", today),
            new ShiftAssignment("assignment-3", "shift-3", "employee-3", "Casey Morgan", today),
            new ShiftAssignment("assignment-4", "shift-1", "employee-4", "Riley Chen", today),
            new ShiftAssignment("assignment-5", "shift-4", "employee-6", "Amelia Ross", today),
            new ShiftAssignment("assignment-6", "shift-2", "employee-9", "Ethan Park", today),
            new ShiftAssignment("assignment-7", "shift-1", "employee-1", "Jordan Lee", yesterday),
            new ShiftAssignment("assignment-8", "shift-1", "employee-14", "Sophia Delgado", yesterday),
            new ShiftAssignment("assignment-9", "shift-1", "employee-2", "Priya Nair", today.AddDays(1)),
            new ShiftAssignment("assignment-10", "shift-3", "employee-3", "Casey Morgan", today.AddDays(1)),
        ];

        _attendance =
        [
            new Attendance("attendance-1", "employee-1", "Jordan Lee", today, new TimeSpan(9, 5, 0), null, AttendanceStatus.Present, string.Empty),
            new Attendance("attendance-2", "employee-2", "Priya Nair", today, new TimeSpan(9, 20, 0), null, AttendanceStatus.Late, string.Empty),
            new Attendance("attendance-3", "employee-3", "Casey Morgan", today, new TimeSpan(10, 0, 0), null, AttendanceStatus.Present, string.Empty),
            new Attendance("attendance-4", "employee-4", "Riley Chen", today, null, null, AttendanceStatus.Absent, "Called in sick."),
            new Attendance("attendance-5", "employee-6", "Amelia Ross", today, new TimeSpan(8, 35, 0), null, AttendanceStatus.Present, string.Empty),
            new Attendance("attendance-6", "employee-9", "Ethan Park", today, new TimeSpan(9, 15, 0), null, AttendanceStatus.Late, string.Empty),
            new Attendance("attendance-7", "employee-17", "Isabella Cruz", today, null, null, AttendanceStatus.Vacation, "Approved leave."),
            new Attendance("attendance-8", "employee-1", "Jordan Lee", yesterday, new TimeSpan(8, 58, 0), new TimeSpan(17, 5, 0), AttendanceStatus.Present, string.Empty),
            new Attendance("attendance-9", "employee-14", "Sophia Delgado", yesterday, new TimeSpan(9, 30, 0), new TimeSpan(17, 0, 0), AttendanceStatus.Late, string.Empty),
        ];

        _leaveRequests =
        [
            new LeaveRequest("leave-1", "employee-17", "Isabella Cruz", today.AddDays(-2), today.AddDays(3), "Family emergency.", LeaveStatus.Approved, now.AddDays(-5)),
            new LeaveRequest("leave-2", "employee-6", "Amelia Ross", today.AddDays(10), today.AddDays(14), "Vacation.", LeaveStatus.Pending, now.AddDays(-1)),
            new LeaveRequest("leave-3", "employee-9", "Ethan Park", today.AddDays(-20), today.AddDays(-18), "Personal.", LeaveStatus.Rejected, now.AddDays(-25)),
            new LeaveRequest("leave-4", "employee-2", "Priya Nair", today.AddDays(-30), today.AddDays(-28), "Vacation.", LeaveStatus.Approved, now.AddDays(-35)),
        ];

        _commissionRules =
        [
            new CommissionRule("rule-1", "employee-1", "Jordan Lee", CommissionType.Percentage, 0.15m, "Senior colorist rate."),
            new CommissionRule("rule-2", "employee-2", "Priya Nair", CommissionType.Percentage, 0.12m, "Stylist rate."),
            new CommissionRule("rule-3", "employee-3", "Casey Morgan", CommissionType.Percentage, 0.10m, "Standard therapist rate."),
            new CommissionRule("rule-4", "employee-4", "Riley Chen", CommissionType.FixedAmount, 15m, "Junior stylist flat rate."),
        ];

        _commissionTransactions =
        [
            new CommissionTransaction("commission-1", "employee-1", "Jordan Lee", "invoice-3", "Haircut & Style", 89.64m, 13.45m, now.AddDays(-95)),
            new CommissionTransaction("commission-2", "employee-1", "Jordan Lee", "invoice-5", "Colour Touch-Up", 157.68m, 23.65m, now.AddDays(-1)),
            new CommissionTransaction("commission-3", "employee-3", "Casey Morgan", "invoice-1", "Manicure", 69.12m, 6.91m, now.AddDays(-5)),
            new CommissionTransaction("commission-4", "employee-3", "Casey Morgan", "invoice-2", "Facial Renewal", 115.56m, 11.56m, now.AddDays(-10)),
        ];

        var lastMonthDate = now.AddMonths(-1);
        _payrollSummaries =
        [
            new PayrollSummary("payroll-1", "employee-1", "Jordan Lee", lastMonthDate.Month, lastMonthDate.Year, 3200m, 450.00m, 100m, 50m, 3700m, lastMonthDate),
            new PayrollSummary("payroll-2", "employee-3", "Casey Morgan", lastMonthDate.Month, lastMonthDate.Year, 2800m, 310m, 0m, 25m, 3085m, lastMonthDate),
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
