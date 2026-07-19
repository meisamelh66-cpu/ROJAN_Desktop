using Rojan.Desktop.Application.Calendar;

namespace Rojan.Desktop.Presentation.Tests.Calendar;

/// <summary>Records every call it receives so ViewModel tests can assert a command was invoked with the right arguments - same reasoning as Customers.StubCustomerCommandService.</summary>
internal sealed class StubCalendarCommandService : ICalendarCommandService
{
    public List<(string SpecialistId, DateTimeOffset Start, DateTimeOffset End)> ReserveCalls { get; } = [];

    public List<(string SpecialistId, DateTimeOffset Start, DateTimeOffset End)> ReleaseCalls { get; } = [];

    public Task ReserveSlotAsync(string specialistId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken cancellationToken = default)
    {
        ReserveCalls.Add((specialistId, slotStart, slotEnd));
        return Task.CompletedTask;
    }

    public Task ReleaseSlotAsync(string specialistId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken cancellationToken = default)
    {
        ReleaseCalls.Add((specialistId, slotStart, slotEnd));
        return Task.CompletedTask;
    }
}
