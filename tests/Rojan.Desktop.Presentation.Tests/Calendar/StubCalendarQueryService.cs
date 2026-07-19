using Rojan.Desktop.Application.Calendar;

namespace Rojan.Desktop.Presentation.Tests.Calendar;

/// <summary>Configurable <see cref="ICalendarQueryService"/> test double - same reasoning as Customers.StubCustomerQueryService.</summary>
internal sealed class StubCalendarQueryService : ICalendarQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<ScheduledSpecialistDto>>> _getSpecialists;
    private readonly Func<string, DateOnly, CancellationToken, Task<DailyAvailabilityDto>> _getDailyAvailability;

    public StubCalendarQueryService(
        Func<CancellationToken, Task<IReadOnlyList<ScheduledSpecialistDto>>> getSpecialists,
        Func<string, DateOnly, CancellationToken, Task<DailyAvailabilityDto>> getDailyAvailability)
    {
        _getSpecialists = getSpecialists;
        _getDailyAvailability = getDailyAvailability;
    }

    public Task<IReadOnlyList<ScheduledSpecialistDto>> GetScheduledSpecialistsAsync(CancellationToken cancellationToken = default) =>
        _getSpecialists(cancellationToken);

    public Task<DailyAvailabilityDto> GetDailyAvailabilityAsync(string specialistId, DateOnly scheduleDate, CancellationToken cancellationToken = default) =>
        _getDailyAvailability(specialistId, scheduleDate, cancellationToken);
}
