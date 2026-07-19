namespace Rojan.Desktop.Application.HR;

/// <summary>Everything the Employee Profile panel needs, fetched together as a single unit of work - same "profile aggregate" precedent as <c>Accounting.InvoiceProfileDto</c>/<c>Customers.CustomerProfileDto</c>.</summary>
public sealed record EmployeeProfileDto(
    EmployeeDto Employee,
    EmployeeDetailDto? Detail,
    IReadOnlyList<AttendanceDto> RecentAttendance,
    IReadOnlyList<ShiftAssignmentDto> UpcomingShifts,
    IReadOnlyList<LeaveRequestDto> LeaveRequests,
    IReadOnlyList<CommissionTransactionDto> RecentCommissions);
