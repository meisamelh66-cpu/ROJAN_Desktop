using Rojan.Desktop.Application.Calendar;
using DomainCalendar = Rojan.Desktop.Domain.Calendar;

namespace Rojan.Desktop.Application.Tests.Calendar;

public sealed class CalendarCommandServiceTests
{
    private static readonly DateTimeOffset SlotStart = new(2026, 3, 2, 9, 0, 0, DateTimeOffset.Now.Offset);

    [Fact]
    public async Task ReserveSlotAsync_NoConflict_AddsBookedSlot()
    {
        var repository = new StubCalendarRepository();
        var sut = new CalendarCommandService(repository);

        await sut.ReserveSlotAsync("specialist-1", SlotStart, SlotStart.AddMinutes(30));

        var slots = Assert.Single(repository.BookedSlotsBySpecialist["specialist-1"]);
        Assert.Equal(SlotStart, slots.Start);
    }

    [Fact]
    public async Task ReserveSlotAsync_ConflictingSlotAlreadyBooked_ThrowsInvalidOperationException()
    {
        var repository = new StubCalendarRepository();
        repository.BookedSlotsBySpecialist["specialist-1"] =
        [
            new DomainCalendar.TimeSlot(SlotStart, SlotStart.AddMinutes(30)),
        ];
        var sut = new CalendarCommandService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ReserveSlotAsync("specialist-1", SlotStart, SlotStart.AddMinutes(30)));
    }

    [Fact]
    public async Task ReleaseSlotAsync_ExistingSlot_RemovesBookedSlot()
    {
        var repository = new StubCalendarRepository();
        repository.BookedSlotsBySpecialist["specialist-1"] =
        [
            new DomainCalendar.TimeSlot(SlotStart, SlotStart.AddMinutes(30)),
        ];
        var sut = new CalendarCommandService(repository);

        await sut.ReleaseSlotAsync("specialist-1", SlotStart, SlotStart.AddMinutes(30));

        Assert.Empty(repository.BookedSlotsBySpecialist["specialist-1"]);
    }
}
