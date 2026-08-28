using Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

internal sealed class StubShiftCommandService : IShiftCommandService
{
    public List<CreateShiftRequest> CreateRequests { get; } = [];

    public List<AssignShiftRequest> AssignRequests { get; } = [];

    /// <summary>Production Hardening (missing-guard sweep, Wave B): when set, the matching command throws this instead of succeeding. Same seam pattern as Customers.StubCustomerCommandService.CreateCustomerException. The call is still recorded before the throw.</summary>
    public Exception? CreateShiftException { get; set; }

    public Exception? AssignShiftException { get; set; }

    public Task<ShiftDto> CreateShiftAsync(CreateShiftRequest request, CancellationToken cancellationToken = default)
    {
        CreateRequests.Add(request);
        return CreateShiftException is not null
            ? Task.FromException<ShiftDto>(CreateShiftException)
            : Task.FromResult(new ShiftDto("shift-new", request.Label, request.Department, request.StartTime, request.EndTime));
    }

    public Task<ShiftAssignmentDto> AssignShiftAsync(AssignShiftRequest request, CancellationToken cancellationToken = default)
    {
        AssignRequests.Add(request);
        return AssignShiftException is not null
            ? Task.FromException<ShiftAssignmentDto>(AssignShiftException)
            : Task.FromResult(new ShiftAssignmentDto("assignment-new", request.ShiftId, request.EmployeeId, "Test Employee", request.AssignedDate));
    }
}
