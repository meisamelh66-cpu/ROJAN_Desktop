using Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

internal sealed class StubShiftCommandService : IShiftCommandService
{
    public List<CreateShiftRequest> CreateRequests { get; } = [];

    public List<AssignShiftRequest> AssignRequests { get; } = [];

    public Task<ShiftDto> CreateShiftAsync(CreateShiftRequest request, CancellationToken cancellationToken = default)
    {
        CreateRequests.Add(request);
        return Task.FromResult(new ShiftDto("shift-new", request.Label, request.Department, request.StartTime, request.EndTime));
    }

    public Task<ShiftAssignmentDto> AssignShiftAsync(AssignShiftRequest request, CancellationToken cancellationToken = default)
    {
        AssignRequests.Add(request);
        return Task.FromResult(new ShiftAssignmentDto("assignment-new", request.ShiftId, request.EmployeeId, "Test Employee", request.AssignedDate));
    }
}
