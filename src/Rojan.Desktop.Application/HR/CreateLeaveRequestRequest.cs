namespace Rojan.Desktop.Application.HR;

/// <summary>Status always starts <see cref="LeaveStatus.Pending"/> - approval/rejection is a separate <c>IAttendanceCommandService.DecideLeaveRequestAsync</c> call, same request-then-decide shape as every other approval flow in this app.</summary>
public sealed record CreateLeaveRequestRequest(
    string EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason);
