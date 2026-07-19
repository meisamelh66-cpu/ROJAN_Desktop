namespace Rojan.Desktop.Application.HR;

public sealed record AssignShiftRequest(
    string ShiftId,
    string EmployeeId,
    DateOnly AssignedDate);
