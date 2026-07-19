using Rojan.Desktop.Domain.Calendar;

namespace Rojan.Desktop.Application.Tests.Calendar;

/// <summary>In-memory, mutable <see cref="ICalendarRepository"/> test double - same reasoning as Customers.StubCustomerRepository.</summary>
internal sealed class StubCalendarRepository : ICalendarRepository
{
    public List<WorkingSchedule> Schedules { get; } = [];

    public Dictionary<string, List<TimeSlot>> BookedSlotsBySpecialist { get; } = [];

    public Task<IReadOnlyList<WorkingSchedule>> GetWorkingSchedulesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkingSchedule>>(Schedules.ToList());

    public Task<IReadOnlyList<TimeSlot>> GetBookedSlotsAsync(string specialistId, DateOnly scheduleDate, CancellationToken cancellationToken = default)
    {
        if (!BookedSlotsBySpecialist.TryGetValue(specialistId, out var slots))
        {
            return Task.FromResult<IReadOnlyList<TimeSlot>>([]);
        }

        return Task.FromResult<IReadOnlyList<TimeSlot>>(
            slots.Where(slot => DateOnly.FromDateTime(slot.Start.DateTime) == scheduleDate).ToList());
    }

    public Task<TimeSlot> ReserveSlotAsync(string specialistId, TimeSlot slot, CancellationToken cancellationToken = default)
    {
        if (!BookedSlotsBySpecialist.TryGetValue(specialistId, out var slots))
        {
            slots = [];
            BookedSlotsBySpecialist[specialistId] = slots;
        }

        slots.Add(slot);
        return Task.FromResult(slot);
    }

    public Task ReleaseSlotAsync(string specialistId, TimeSlot slot, CancellationToken cancellationToken = default)
    {
        if (BookedSlotsBySpecialist.TryGetValue(specialistId, out var slots))
        {
            slots.RemoveAll(existing => existing.Start == slot.Start && existing.End == slot.End);
        }

        return Task.CompletedTask;
    }
}
