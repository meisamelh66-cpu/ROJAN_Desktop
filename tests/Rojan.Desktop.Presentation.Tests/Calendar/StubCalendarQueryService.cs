using Rojan.Desktop.Application.Calendar;

namespace Rojan.Desktop.Presentation.Tests.Calendar;

/// <summary>Configurable <see cref="ICalendarQueryService"/> test double - same reasoning as Customers.StubCustomerQueryService.</summary>
internal sealed class StubCalendarQueryService : ICalendarQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<ScheduledSpecialistDto>>> _getSpecialists;
    private readonly Func<string, DateOnly, CancellationToken, Task<DailyAvailabilityDto>> _getDailyAvailability;
    private readonly Func<string, DateOnly, CancellationToken, Task<WeeklyAvailabilityDto>> _getWeeklyAvailability;

    public StubCalendarQueryService(
        Func<CancellationToken, Task<IReadOnlyList<ScheduledSpecialistDto>>> getSpecialists,
        Func<string, DateOnly, CancellationToken, Task<DailyAvailabilityDto>> getDailyAvailability,
        Func<string, DateOnly, CancellationToken, Task<WeeklyAvailabilityDto>>? getWeeklyAvailability = null)
    {
        _getSpecialists = getSpecialists;
        _getDailyAvailability = getDailyAvailability;

        // Sprint 2 Commit 3 introduced this interface member with no
        // Presentation consumer yet (that's Commit 4) - default composes it
        // from the already-required daily getter so none of this stub's
        // existing eight call sites need updating.
        _getWeeklyAvailability = getWeeklyAvailability ?? DefaultGetWeeklyAvailabilityAsync;
    }

    public Task<IReadOnlyList<ScheduledSpecialistDto>> GetScheduledSpecialistsAsync(CancellationToken cancellationToken = default) =>
        _getSpecialists(cancellationToken);

    public Task<DailyAvailabilityDto> GetDailyAvailabilityAsync(string specialistId, DateOnly scheduleDate, CancellationToken cancellationToken = default) =>
        _getDailyAvailability(specialistId, scheduleDate, cancellationToken);

    public Task<WeeklyAvailabilityDto> GetWeeklyAvailabilityAsync(string specialistId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        _getWeeklyAvailability(specialistId, weekStart, cancellationToken);

    private async Task<WeeklyAvailabilityDto> DefaultGetWeeklyAvailabilityAsync(string specialistId, DateOnly weekStart, CancellationToken cancellationToken)
    {
        var days = new List<DailyAvailabilityDto>(7);
        for (var offset = 0; offset < 7; offset++)
        {
            days.Add(await _getDailyAvailability(specialistId, weekStart.AddDays(offset), cancellationToken).ConfigureAwait(true));
        }

        var specialistName = days.Count > 0 ? days[0].SpecialistName : string.Empty;
        return new WeeklyAvailabilityDto(specialistId, specialistName, weekStart, days);
    }
}
