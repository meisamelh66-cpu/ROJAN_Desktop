using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.HR;

/// <summary>Domain &lt;-&gt; Application mapping for the HR slice - same reasoning/shape as every other module's <c>*Mapper</c>.</summary>
internal static class HrMapper
{
    public static EmployeeDto MapEmployee(DomainHr.Employee employee) => new(
        employee.Id, employee.SpecialistId, employee.FullName, employee.Email, employee.Phone,
        MapRole(employee.Role), MapDepartment(employee.Department), MapEmploymentType(employee.EmploymentType),
        MapStatus(employee.Status), employee.HireDate, employee.BaseSalary);

    public static EmployeeDetailDto MapDetail(DomainHr.EmployeeProfile profile) => new(
        profile.Id, profile.EmployeeId, profile.Bio, profile.Skills, profile.EmergencyContactName, profile.EmergencyContactPhone);

    public static ShiftDto MapShift(DomainHr.Shift shift) => new(
        shift.Id, shift.Label, MapDepartment(shift.Department), shift.StartTime, shift.EndTime);

    public static ShiftAssignmentDto MapShiftAssignment(DomainHr.ShiftAssignment assignment) => new(
        assignment.Id, assignment.ShiftId, assignment.EmployeeId, assignment.EmployeeName, assignment.AssignedDate);

    public static AttendanceDto MapAttendance(DomainHr.Attendance attendance) => new(
        attendance.Id, attendance.EmployeeId, attendance.EmployeeName, attendance.Date,
        attendance.CheckInTime, attendance.CheckOutTime, MapAttendanceStatus(attendance.Status), attendance.Notes);

    public static LeaveRequestDto MapLeaveRequest(DomainHr.LeaveRequest leaveRequest) => new(
        leaveRequest.Id, leaveRequest.EmployeeId, leaveRequest.EmployeeName, leaveRequest.StartDate, leaveRequest.EndDate,
        leaveRequest.Reason, MapLeaveStatus(leaveRequest.Status), leaveRequest.RequestedAt);

    public static CommissionRuleDto MapCommissionRule(DomainHr.CommissionRule rule) => new(
        rule.Id, rule.EmployeeId, rule.EmployeeName, MapCommissionType(rule.Type), rule.Value, rule.Description);

    public static CommissionTransactionDto MapCommissionTransaction(DomainHr.CommissionTransaction transaction) => new(
        transaction.Id, transaction.EmployeeId, transaction.EmployeeName, transaction.InvoiceId, transaction.ServiceName,
        transaction.GrossAmount, transaction.CommissionAmount, transaction.EarnedAt);

    public static PayrollSummaryDto MapPayrollSummary(DomainHr.PayrollSummary summary) => new(
        summary.Id, summary.EmployeeId, summary.EmployeeName, summary.Month, summary.Year,
        summary.BaseSalary, summary.CommissionTotal, summary.Bonus, summary.Deduction, summary.NetSalary, summary.GeneratedAt);

    public static DomainHr.EmployeeStatus MapStatusToDomain(EmployeeStatus status) => status switch
    {
        EmployeeStatus.Active => DomainHr.EmployeeStatus.Active,
        EmployeeStatus.Inactive => DomainHr.EmployeeStatus.Inactive,
        EmployeeStatus.Suspended => DomainHr.EmployeeStatus.Suspended,
        EmployeeStatus.OnLeave => DomainHr.EmployeeStatus.OnLeave,
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static DomainHr.Department MapDepartmentToDomain(Department department) => department switch
    {
        Department.Reception => DomainHr.Department.Reception,
        Department.Hair => DomainHr.Department.Hair,
        Department.Nails => DomainHr.Department.Nails,
        Department.Makeup => DomainHr.Department.Makeup,
        Department.SkinCare => DomainHr.Department.SkinCare,
        Department.Massage => DomainHr.Department.Massage,
        Department.Management => DomainHr.Department.Management,
        _ => throw new ArgumentOutOfRangeException(nameof(department)),
    };

    public static DomainHr.EmploymentType MapEmploymentTypeToDomain(EmploymentType type) => type switch
    {
        EmploymentType.FullTime => DomainHr.EmploymentType.FullTime,
        EmploymentType.PartTime => DomainHr.EmploymentType.PartTime,
        EmploymentType.Contractor => DomainHr.EmploymentType.Contractor,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static DomainHr.EmployeeRole MapRoleToDomain(EmployeeRole role) => role switch
    {
        EmployeeRole.Stylist => DomainHr.EmployeeRole.Stylist,
        EmployeeRole.Colorist => DomainHr.EmployeeRole.Colorist,
        EmployeeRole.NailTechnician => DomainHr.EmployeeRole.NailTechnician,
        EmployeeRole.Esthetician => DomainHr.EmployeeRole.Esthetician,
        EmployeeRole.MassageTherapist => DomainHr.EmployeeRole.MassageTherapist,
        EmployeeRole.Receptionist => DomainHr.EmployeeRole.Receptionist,
        EmployeeRole.Manager => DomainHr.EmployeeRole.Manager,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    public static DomainHr.AttendanceStatus MapAttendanceStatusToDomain(AttendanceStatus status) => status switch
    {
        AttendanceStatus.Present => DomainHr.AttendanceStatus.Present,
        AttendanceStatus.Late => DomainHr.AttendanceStatus.Late,
        AttendanceStatus.Absent => DomainHr.AttendanceStatus.Absent,
        AttendanceStatus.Vacation => DomainHr.AttendanceStatus.Vacation,
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static DomainHr.LeaveStatus MapLeaveStatusToDomain(LeaveStatus status) => status switch
    {
        LeaveStatus.Pending => DomainHr.LeaveStatus.Pending,
        LeaveStatus.Approved => DomainHr.LeaveStatus.Approved,
        LeaveStatus.Rejected => DomainHr.LeaveStatus.Rejected,
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static DomainHr.CommissionType MapCommissionTypeToDomain(CommissionType type) => type switch
    {
        CommissionType.FixedAmount => DomainHr.CommissionType.FixedAmount,
        CommissionType.Percentage => DomainHr.CommissionType.Percentage,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    private static EmployeeStatus MapStatus(DomainHr.EmployeeStatus status) => status switch
    {
        DomainHr.EmployeeStatus.Active => EmployeeStatus.Active,
        DomainHr.EmployeeStatus.Inactive => EmployeeStatus.Inactive,
        DomainHr.EmployeeStatus.Suspended => EmployeeStatus.Suspended,
        DomainHr.EmployeeStatus.OnLeave => EmployeeStatus.OnLeave,
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    private static Department MapDepartment(DomainHr.Department department) => department switch
    {
        DomainHr.Department.Reception => Department.Reception,
        DomainHr.Department.Hair => Department.Hair,
        DomainHr.Department.Nails => Department.Nails,
        DomainHr.Department.Makeup => Department.Makeup,
        DomainHr.Department.SkinCare => Department.SkinCare,
        DomainHr.Department.Massage => Department.Massage,
        DomainHr.Department.Management => Department.Management,
        _ => throw new ArgumentOutOfRangeException(nameof(department)),
    };

    private static EmploymentType MapEmploymentType(DomainHr.EmploymentType type) => type switch
    {
        DomainHr.EmploymentType.FullTime => EmploymentType.FullTime,
        DomainHr.EmploymentType.PartTime => EmploymentType.PartTime,
        DomainHr.EmploymentType.Contractor => EmploymentType.Contractor,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    private static EmployeeRole MapRole(DomainHr.EmployeeRole role) => role switch
    {
        DomainHr.EmployeeRole.Stylist => EmployeeRole.Stylist,
        DomainHr.EmployeeRole.Colorist => EmployeeRole.Colorist,
        DomainHr.EmployeeRole.NailTechnician => EmployeeRole.NailTechnician,
        DomainHr.EmployeeRole.Esthetician => EmployeeRole.Esthetician,
        DomainHr.EmployeeRole.MassageTherapist => EmployeeRole.MassageTherapist,
        DomainHr.EmployeeRole.Receptionist => EmployeeRole.Receptionist,
        DomainHr.EmployeeRole.Manager => EmployeeRole.Manager,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    private static AttendanceStatus MapAttendanceStatus(DomainHr.AttendanceStatus status) => status switch
    {
        DomainHr.AttendanceStatus.Present => AttendanceStatus.Present,
        DomainHr.AttendanceStatus.Late => AttendanceStatus.Late,
        DomainHr.AttendanceStatus.Absent => AttendanceStatus.Absent,
        DomainHr.AttendanceStatus.Vacation => AttendanceStatus.Vacation,
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    private static LeaveStatus MapLeaveStatus(DomainHr.LeaveStatus status) => status switch
    {
        DomainHr.LeaveStatus.Pending => LeaveStatus.Pending,
        DomainHr.LeaveStatus.Approved => LeaveStatus.Approved,
        DomainHr.LeaveStatus.Rejected => LeaveStatus.Rejected,
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    private static CommissionType MapCommissionType(DomainHr.CommissionType type) => type switch
    {
        DomainHr.CommissionType.FixedAmount => CommissionType.FixedAmount,
        DomainHr.CommissionType.Percentage => CommissionType.Percentage,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };
}
