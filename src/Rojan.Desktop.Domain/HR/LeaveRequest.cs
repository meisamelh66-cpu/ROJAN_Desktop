namespace Rojan.Desktop.Domain.HR;

public sealed record LeaveRequest(
    string Id,
    string EmployeeId,
    string EmployeeName,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    LeaveStatus Status,
    DateTimeOffset RequestedAt);
