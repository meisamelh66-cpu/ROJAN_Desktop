namespace Rojan.Desktop.Application.HR;

/// <summary>Write use cases for Shifts - template creation and employee assignment.</summary>
public interface IShiftCommandService
{
    public Task<ShiftDto> CreateShiftAsync(CreateShiftRequest request, CancellationToken cancellationToken = default);

    public Task<ShiftAssignmentDto> AssignShiftAsync(AssignShiftRequest request, CancellationToken cancellationToken = default);
}
