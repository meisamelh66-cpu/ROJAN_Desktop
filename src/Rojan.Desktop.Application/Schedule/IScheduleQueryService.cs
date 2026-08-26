namespace Rojan.Desktop.Application.Schedule;

/// <summary>Read surface for a specialist's real schedule - see <see cref="IScheduleRepository"/>'s own doc comment for the authority reasoning. Ungated: Backend itself already redacts <c>Reason</c> fields per-viewer (see <see cref="ScheduleOverrideDto"/>'s own doc comment), same "no local permission duplication for reads" convention <c>Bookings</c>/<c>Calendar</c> already established.</summary>
public interface IScheduleQueryService
{
    public Task<IReadOnlyList<WeeklyAvailabilityDto>> GetWeeklyAvailabilityAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ScheduleOverrideDto>> GetOverridesAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SpecialistLeaveDto>> GetLeavesAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SpecialistBlockDto>> GetBlocksAsync(string specialistId, CancellationToken cancellationToken = default);
}
