using Rojan.Desktop.Application.Calendar;

namespace Rojan.Desktop.Application.Tests.BookingWorkflow;

/// <summary>Configurable <see cref="ICalendarQueryService"/> test double - same reasoning as Presentation.Tests.Calendar.StubCalendarQueryService, duplicated here since internal test doubles don't cross assemblies.</summary>
internal sealed class StubCalendarQueryService : ICalendarQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<ScheduledSpecialistDto>>> _getSpecialists;
    private readonly Func<string, DateOnly, CancellationToken, Task<DailyAvailabilityDto>> _getDailyAvailability;

    public StubCalendarQueryService(
        Func<CancellationToken, Task<IReadOnlyList<ScheduledSpecialistDto>>>? getSpecialists = null,
        Func<string, DateOnly, CancellationToken, Task<DailyAvailabilityDto>>? getDailyAvailability = null)
    {
        _getSpecialists = getSpecialists ?? (_ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>([]));
        _getDailyAvailability = getDailyAvailability
            ?? ((specialistId, date, _) => Task.FromResult(new DailyAvailabilityDto(specialistId, string.Empty, date, null, null, [])));
    }

    public Task<IReadOnlyList<ScheduledSpecialistDto>> GetScheduledSpecialistsAsync(CancellationToken cancellationToken = default) =>
        _getSpecialists(cancellationToken);

    public Task<DailyAvailabilityDto> GetDailyAvailabilityAsync(string specialistId, DateOnly scheduleDate, CancellationToken cancellationToken = default) =>
        _getDailyAvailability(specialistId, scheduleDate, cancellationToken);
}
