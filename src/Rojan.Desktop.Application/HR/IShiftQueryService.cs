namespace Rojan.Desktop.Application.HR;

/// <summary>Read-only use cases Presentation depends on to load Shifts and their assignments.</summary>
public interface IShiftQueryService
{
    public Task<IReadOnlyList<ShiftDto>> GetShiftsAsync(CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ShiftAssignmentDto>> GetShiftAssignmentsForEmployeeAsync(string employeeId, CancellationToken cancellationToken = default);

    /// <summary>Every scheduled assignment - backs the Shifts page's roster view.</summary>
    public Task<IReadOnlyList<ShiftAssignmentDto>> GetAllShiftAssignmentsAsync(CancellationToken cancellationToken = default);
}
