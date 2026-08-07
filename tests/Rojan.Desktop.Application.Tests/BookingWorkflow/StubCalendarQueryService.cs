using Rojan.Desktop.Application.Calendar;

namespace Rojan.Desktop.Application.Tests.BookingWorkflow;

/// <summary>Configurable <see cref="ICalendarQueryService"/> test double - same reasoning as Presentation.Tests.Calendar.StubCalendarQueryService, duplicated here since internal test doubles don't cross assemblies.</summary>
internal sealed class StubCalendarQueryService : ICalendarQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<ScheduledSpecialistDto>>> _getSpecialists;
    private readonly Func<string, string, DateOnly, CancellationToken, Task<DailyAvailabilityDto>> _getDailyAvailability;
    private readonly Func<string, string, DateOnly, CancellationToken, Task<WeeklyAvailabilityDto>> _getWeeklyAvailability;

    public StubCalendarQueryService(
        Func<CancellationToken, Task<IReadOnlyList<ScheduledSpecialistDto>>>? getSpecialists = null,
        Func<string, string, DateOnly, CancellationToken, Task<DailyAvailabilityDto>>? getDailyAvailability = null,
        Func<string, string, DateOnly, CancellationToken, Task<WeeklyAvailabilityDto>>? getWeeklyAvailability = null)
    {
        _getSpecialists = getSpecialists ?? (_ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>([]));
        _getDailyAvailability = getDailyAvailability
            ?? ((specialistId, _, date, _) => Task.FromResult(new DailyAvailabilityDto(specialistId, string.Empty, date, null, null, [])));
        _getWeeklyAvailability = getWeeklyAvailability
            ?? ((specialistId, _, weekStart, _) => Task.FromResult(new WeeklyAvailabilityDto(specialistId, string.Empty, weekStart, [])));
    }

    public Task<IReadOnlyList<ScheduledSpecialistDto>> GetScheduledSpecialistsAsync(CancellationToken cancellationToken = default) =>
        _getSpecialists(cancellationToken);

    public Task<DailyAvailabilityDto> GetDailyAvailabilityAsync(string specialistId, string serviceId, DateOnly scheduleDate, CancellationToken cancellationToken = default) =>
        _getDailyAvailability(specialistId, serviceId, scheduleDate, cancellationToken);

    public Task<WeeklyAvailabilityDto> GetWeeklyAvailabilityAsync(string specialistId, string serviceId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        _getWeeklyAvailability(specialistId, serviceId, weekStart, cancellationToken);
}
