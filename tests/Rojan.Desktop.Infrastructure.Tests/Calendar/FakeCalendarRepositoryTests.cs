using Rojan.Desktop.Domain.Calendar;
using Rojan.Desktop.Infrastructure.Calendar;

namespace Rojan.Desktop.Infrastructure.Tests.Calendar;

/// <summary>Smoke + behavioral coverage - same reasoning as Customers.FakeCustomerRepositoryTests.</summary>
public sealed class FakeCalendarRepositoryTests
{
    [Fact]
    public async Task GetWorkingSchedulesAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeCalendarRepository();

        var result = await sut.GetWorkingSchedulesAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetWorkingSchedulesAsync_CancellationAlreadyRequested_ThrowsTaskCanceledException()
    {
        var sut = new FakeCalendarRepository();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => sut.GetWorkingSchedulesAsync(cts.Token));
    }

    [Fact]
    public async Task GetWorkingSchedulesAsync_IncludesEveryActiveSpecialist()
    {
        var sut = new FakeCalendarRepository();

        var result = await sut.GetWorkingSchedulesAsync();

        var specialistNames = result.Select(schedule => schedule.SpecialistName).Distinct().ToList();
        Assert.Contains("کیانا رادمنش", specialistNames);
        Assert.Contains("سارا امینی", specialistNames);
        Assert.Contains("مهسا کریمی", specialistNames);
    }

    [Fact]
    public async Task GetBookedSlotsAsync_SeededSpecialist_HasAtLeastOneConflictWithinTheNextWeek()
    {
        var sut = new FakeCalendarRepository();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var foundAny = false;
        for (var offset = 0; offset <= 7; offset++)
        {
            var slots = await sut.GetBookedSlotsAsync("specialist-1", today.AddDays(offset));
            if (slots.Count > 0)
            {
                foundAny = true;
                break;
            }
        }

        Assert.True(foundAny, "Expected FakeCalendarRepository's seed data to include at least one booked slot for specialist-1 within the next week.");
    }

    [Fact]
    public async Task ReserveSlotAsync_NewSlot_BecomesVisibleViaGetBookedSlotsAsync()
    {
        var sut = new FakeCalendarRepository();
        var farFutureDate = DateOnly.FromDateTime(DateTime.Today).AddDays(60);
        var start = new DateTimeOffset(farFutureDate.Year, farFutureDate.Month, farFutureDate.Day, 9, 0, 0, DateTimeOffset.Now.Offset);
        var slot = new TimeSlot(start, start.AddMinutes(30));

        await sut.ReserveSlotAsync("specialist-1", slot);
        var slots = await sut.GetBookedSlotsAsync("specialist-1", farFutureDate);

        Assert.Contains(slots, s => s.Start == slot.Start && s.End == slot.End);
    }

    [Fact]
    public async Task ReleaseSlotAsync_ExistingSlot_NoLongerReturnedByGetBookedSlotsAsync()
    {
        var sut = new FakeCalendarRepository();
        var farFutureDate = DateOnly.FromDateTime(DateTime.Today).AddDays(61);
        var start = new DateTimeOffset(farFutureDate.Year, farFutureDate.Month, farFutureDate.Day, 9, 0, 0, DateTimeOffset.Now.Offset);
        var slot = new TimeSlot(start, start.AddMinutes(30));
        await sut.ReserveSlotAsync("specialist-1", slot);

        await sut.ReleaseSlotAsync("specialist-1", slot);
        var slots = await sut.GetBookedSlotsAsync("specialist-1", farFutureDate);

        Assert.DoesNotContain(slots, s => s.Start == slot.Start && s.End == slot.End);
    }
}
