using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.HR;

/// <summary>Default <see cref="IShiftCommandService"/> implementation.</summary>
public sealed class ShiftCommandService : IShiftCommandService
{
    private readonly DomainHr.IHrRepository _repository;

    public ShiftCommandService(DomainHr.IHrRepository repository)
    {
        _repository = repository;
    }

    public async Task<ShiftDto> CreateShiftAsync(CreateShiftRequest request, CancellationToken cancellationToken = default)
    {
        var shift = new DomainHr.Shift(
            Guid.NewGuid().ToString(), request.Label, HrMapper.MapDepartmentToDomain(request.Department),
            request.StartTime, request.EndTime);

        var created = await _repository.CreateShiftAsync(shift, cancellationToken).ConfigureAwait(true);
        return HrMapper.MapShift(created);
    }

    public async Task<ShiftAssignmentDto> AssignShiftAsync(AssignShiftRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetEmployeeByIdAsync(request.EmployeeId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Employee '{request.EmployeeId}' was not found.");

        var assignment = new DomainHr.ShiftAssignment(
            Guid.NewGuid().ToString(), request.ShiftId, employee.Id, employee.FullName, request.AssignedDate);

        var created = await _repository.CreateShiftAssignmentAsync(assignment, cancellationToken).ConfigureAwait(true);
        return HrMapper.MapShiftAssignment(created);
    }
}
