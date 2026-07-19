using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.HR;

/// <summary>Default <see cref="IShiftQueryService"/> implementation.</summary>
public sealed class ShiftQueryService : IShiftQueryService
{
    private readonly DomainHr.IHrRepository _repository;

    public ShiftQueryService(DomainHr.IHrRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ShiftDto>> GetShiftsAsync(CancellationToken cancellationToken = default)
    {
        var shifts = await _repository.GetShiftsAsync(cancellationToken).ConfigureAwait(true);
        return shifts.Select(HrMapper.MapShift).ToList();
    }

    public async Task<IReadOnlyList<ShiftAssignmentDto>> GetShiftAssignmentsForEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        var assignments = await _repository.GetShiftAssignmentsAsync(cancellationToken).ConfigureAwait(true);
        return assignments.Where(a => a.EmployeeId == employeeId).OrderBy(a => a.AssignedDate).Select(HrMapper.MapShiftAssignment).ToList();
    }

    public async Task<IReadOnlyList<ShiftAssignmentDto>> GetAllShiftAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        var assignments = await _repository.GetShiftAssignmentsAsync(cancellationToken).ConfigureAwait(true);
        return assignments.OrderBy(a => a.AssignedDate).Select(HrMapper.MapShiftAssignment).ToList();
    }
}
