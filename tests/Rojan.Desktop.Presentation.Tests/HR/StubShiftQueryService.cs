using Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

internal sealed class StubShiftQueryService : IShiftQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<ShiftDto>>>? _getShifts;
    private readonly Func<CancellationToken, Task<IReadOnlyList<ShiftAssignmentDto>>>? _getAllAssignments;

    public StubShiftQueryService(
        Func<CancellationToken, Task<IReadOnlyList<ShiftDto>>>? getShifts = null,
        Func<CancellationToken, Task<IReadOnlyList<ShiftAssignmentDto>>>? getAllAssignments = null)
    {
        _getShifts = getShifts;
        _getAllAssignments = getAllAssignments;
    }

    public Task<IReadOnlyList<ShiftDto>> GetShiftsAsync(CancellationToken cancellationToken = default) =>
        _getShifts?.Invoke(cancellationToken) ?? Task.FromResult<IReadOnlyList<ShiftDto>>([]);

    public Task<IReadOnlyList<ShiftAssignmentDto>> GetShiftAssignmentsForEmployeeAsync(string employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ShiftAssignmentDto>>([]);

    public Task<IReadOnlyList<ShiftAssignmentDto>> GetAllShiftAssignmentsAsync(CancellationToken cancellationToken = default) =>
        _getAllAssignments?.Invoke(cancellationToken) ?? Task.FromResult<IReadOnlyList<ShiftAssignmentDto>>([]);
}
