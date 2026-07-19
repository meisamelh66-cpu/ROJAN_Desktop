namespace Rojan.Desktop.Domain.HR;

/// <summary>One employee scheduled onto one <see cref="Shift"/> for one date - the Calendar integration point ("Shift Assignment" feeds Attendance expectations for that date).</summary>
public sealed record ShiftAssignment(
    string Id,
    string ShiftId,
    string EmployeeId,
    string EmployeeName,
    DateOnly AssignedDate);
