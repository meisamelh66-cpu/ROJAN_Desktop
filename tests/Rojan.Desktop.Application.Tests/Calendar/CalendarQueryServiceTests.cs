using Rojan.Desktop.Application.Calendar;
using DomainCalendar = Rojan.Desktop.Domain.Calendar;

namespace Rojan.Desktop.Application.Tests.Calendar;

public sealed class CalendarQueryServiceTests
{
    private static readonly DateOnly TestDate = new(2026, 3, 2);

    private static DayOfWeek OtherDayOfWeek(DateOnly date) => (DayOfWeek)(((int)date.DayOfWeek + 1) % 7);

    [Fact]
    public async Task GetScheduledSpecialistsAsync_ReturnsDistinctSpecialistsOrderedByName()
    {
        var repository = new StubCalendarRepository();
        repository.Schedules.Add(new DomainCalendar.WorkingSchedule("s-1", "specialist-2", "Priya Nair", DayOfWeek.Monday, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)));
        repository.Schedules.Add(new DomainCalendar.WorkingSchedule("s-2", "specialist-2", "Priya Nair", DayOfWeek.Tuesday, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)));
        repository.Schedules.Add(new DomainCalendar.WorkingSchedule("s-3", "specialist-1", "Jordan Lee", DayOfWeek.Monday, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)));
        var sut = new CalendarQueryService(repository);

        var result = await sut.GetScheduledSpecialistsAsync();

        Assert.Equal(["Jordan Lee", "Priya Nair"], result.Select(specialist => specialist.Name));
    }

    [Fact]
    public async Task GetDailyAvailabilityAsync_SpecialistHasNoSchedule_ThrowsInvalidOperationException()
    {
        var repository = new StubCalendarRepository();
        var sut = new CalendarQueryService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetDailyAvailabilityAsync("no-such-specialist", "service-1", TestDate));
    }

    [Fact]
    public async Task GetDailyAvailabilityAsync_SpecialistDoesNotWorkThatDay_ReturnsEmptySlotsAndNullWorkingHours()
    {
        var repository = new StubCalendarRepository();
        repository.Schedules.Add(new DomainCalendar.WorkingSchedule(
            "s-1", "specialist-1", "Jordan Lee", OtherDayOfWeek(TestDate), new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)));
        var sut = new CalendarQueryService(repository);

        var result = await sut.GetDailyAvailabilityAsync("specialist-1", "service-1", TestDate);

        Assert.Empty(result.Slots);
        Assert.Null(result.WorkingStart);
        Assert.Null(result.WorkingEnd);
        Assert.Equal("Jordan Lee", result.SpecialistName);
    }

    [Fact]
    public async Task GetDailyAvailabilityAsync_WorkingDay_GeneratesThirtyMinuteSlotsAcrossWorkingHours()
    {
        var repository = new StubCalendarRepository();
        repository.Schedules.Add(new DomainCalendar.WorkingSchedule(
            "s-1", "specialist-1", "Jordan Lee", TestDate.DayOfWeek, new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0)));
        var sut = new CalendarQueryService(repository);

        var result = await sut.GetDailyAvailabilityAsync("specialist-1", "service-1", TestDate);

        Assert.Equal(4, result.Slots.Count);
        Assert.All(result.Slots, slot => Assert.Equal(TimeSpan.FromMinutes(30), slot.End - slot.Start));
        Assert.All(result.Slots, slot => Assert.Equal(AvailabilityStatus.Available, slot.Status));
    }

    [Fact]
    public async Task GetDailyAvailabilityAsync_SlotOverlapsBookedRange_MarksOnlyThatSlotAsBooked()
    {
        var repository = new StubCalendarRepository();
        repository.Schedules.Add(new DomainCalendar.WorkingSchedule(
            "s-1", "specialist-1", "Jordan Lee", TestDate.DayOfWeek, new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0)));
        // Must match CalendarQueryService's own DateTimeOffset.Now.Offset convention -
        // comparing against a different offset would compare different instants
        // even though the displayed hour/minute look the same.
        var bookedStart = new DateTimeOffset(TestDate.Year, TestDate.Month, TestDate.Day, 9, 30, 0, DateTimeOffset.Now.Offset);
        repository.BookedSlotsBySpecialist["specialist-1"] =
        [
            new DomainCalendar.TimeSlot(bookedStart, bookedStart.AddMinutes(30)),
        ];
        var sut = new CalendarQueryService(repository);

        var result = await sut.GetDailyAvailabilityAsync("specialist-1", "service-1", TestDate);

        var bookedSlot = Assert.Single(result.Slots, slot => slot.Status == AvailabilityStatus.Booked);
        Assert.Equal(9, bookedSlot.Start.Hour);
        Assert.Equal(30, bookedSlot.Start.Minute);
        Assert.Equal(3, result.Slots.Count(slot => slot.Status == AvailabilityStatus.Available));
    }

    [Fact]
    public async Task GetDailyAvailabilityAsync_SlotOverlapsBreak_MarksOnlyThatSlotAsUnavailable()
    {
        var repository = new StubCalendarRepository();
        var breakStart = new TimeSpan(9, 30, 0);
        repository.Schedules.Add(new DomainCalendar.WorkingSchedule(
            "s-1", "specialist-1", "Jordan Lee", TestDate.DayOfWeek, new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0),
            [new DomainCalendar.TimeSlot(
                new DateTimeOffset(TestDate.Year, TestDate.Month, TestDate.Day, breakStart.Hours, breakStart.Minutes, 0, DateTimeOffset.Now.Offset),
                new DateTimeOffset(TestDate.Year, TestDate.Month, TestDate.Day, breakStart.Hours, breakStart.Minutes, 0, DateTimeOffset.Now.Offset).AddMinutes(30))]));
        var sut = new CalendarQueryService(repository);

        var result = await sut.GetDailyAvailabilityAsync("specialist-1", "service-1", TestDate);

        var unavailableSlot = Assert.Single(result.Slots, slot => slot.Status == AvailabilityStatus.Unavailable);
        Assert.Equal(9, unavailableSlot.Start.Hour);
        Assert.Equal(30, unavailableSlot.Start.Minute);
        Assert.Equal(3, result.Slots.Count(slot => slot.Status == AvailabilityStatus.Available));
    }

    [Fact]
    public async Task GetDailyAvailabilityAsync_BreakOverlapsBookedSlot_MarksUnavailableNotBooked()
    {
        var repository = new StubCalendarRepository();
        var overlapStart = new DateTimeOffset(TestDate.Year, TestDate.Month, TestDate.Day, 9, 30, 0, DateTimeOffset.Now.Offset);
        repository.Schedules.Add(new DomainCalendar.WorkingSchedule(
            "s-1", "specialist-1", "Jordan Lee", TestDate.DayOfWeek, new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0),
            [new DomainCalendar.TimeSlot(overlapStart, overlapStart.AddMinutes(30))]));
        repository.BookedSlotsBySpecialist["specialist-1"] =
        [
            new DomainCalendar.TimeSlot(overlapStart, overlapStart.AddMinutes(30)),
        ];
        var sut = new CalendarQueryService(repository);

        var result = await sut.GetDailyAvailabilityAsync("specialist-1", "service-1", TestDate);

        var overlapSlot = Assert.Single(result.Slots, slot => slot.Start == overlapStart);
        Assert.Equal(AvailabilityStatus.Unavailable, overlapSlot.Status);
    }

    [Fact]
    public async Task GetWeeklyAvailabilityAsync_SpecialistHasNoSchedule_ThrowsInvalidOperationException()
    {
        var repository = new StubCalendarRepository();
        var sut = new CalendarQueryService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetWeeklyAvailabilityAsync("no-such-specialist", "service-1", TestDate));
    }

    [Fact]
    public async Task GetWeeklyAvailabilityAsync_ReturnsSevenDaysStartingAtWeekStart_InOrder()
    {
        var repository = new StubCalendarRepository();
        repository.Schedules.Add(new DomainCalendar.WorkingSchedule(
            "s-1", "specialist-1", "Jordan Lee", TestDate.DayOfWeek, new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0)));
        var sut = new CalendarQueryService(repository);

        var result = await sut.GetWeeklyAvailabilityAsync("specialist-1", "service-1", TestDate);

        Assert.Equal("specialist-1", result.SpecialistId);
        Assert.Equal("Jordan Lee", result.SpecialistName);
        Assert.Equal(TestDate, result.WeekStart);
        Assert.Equal(7, result.Days.Count);
        Assert.Equal(Enumerable.Range(0, 7).Select(offset => TestDate.AddDays(offset)), result.Days.Select(day => day.Date));
    }

    [Fact]
    public async Task GetWeeklyAvailabilityAsync_DayNotWorked_HasEmptySlotsAndNullWorkingHours()
    {
        var repository = new StubCalendarRepository();
        repository.Schedules.Add(new DomainCalendar.WorkingSchedule(
            "s-1", "specialist-1", "Jordan Lee", TestDate.DayOfWeek, new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0)));
        var sut = new CalendarQueryService(repository);

        var result = await sut.GetWeeklyAvailabilityAsync("specialist-1", "service-1", TestDate);

        var otherDay = result.Days.Single(day => day.Date == TestDate.AddDays(1));
        Assert.Empty(otherDay.Slots);
        Assert.Null(otherDay.WorkingStart);
        Assert.Null(otherDay.WorkingEnd);
    }

    [Fact]
    public async Task GetWeeklyAvailabilityAsync_ReflectsBookedAndBreakStatusesOnTheirRespectiveDays()
    {
        var repository = new StubCalendarRepository();
        var bookedDay = TestDate.AddDays(1);
        var breakStart = new DateTimeOffset(TestDate.Year, TestDate.Month, TestDate.Day, 9, 30, 0, DateTimeOffset.Now.Offset);
        var bookedStart = new DateTimeOffset(bookedDay.Year, bookedDay.Month, bookedDay.Day, 9, 30, 0, DateTimeOffset.Now.Offset);

        repository.Schedules.Add(new DomainCalendar.WorkingSchedule(
            "s-1", "specialist-1", "Jordan Lee", TestDate.DayOfWeek, new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0),
            [new DomainCalendar.TimeSlot(breakStart, breakStart.AddMinutes(30))]));
        repository.Schedules.Add(new DomainCalendar.WorkingSchedule(
            "s-2", "specialist-1", "Jordan Lee", bookedDay.DayOfWeek, new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0)));
        repository.BookedSlotsBySpecialist["specialist-1"] =
        [
            new DomainCalendar.TimeSlot(bookedStart, bookedStart.AddMinutes(30)),
        ];
        var sut = new CalendarQueryService(repository);

        var result = await sut.GetWeeklyAvailabilityAsync("specialist-1", "service-1", TestDate);

        var breakDayResult = result.Days.Single(day => day.Date == TestDate);
        Assert.Contains(breakDayResult.Slots, slot => slot.Status == AvailabilityStatus.Unavailable);

        var bookedDayResult = result.Days.Single(day => day.Date == bookedDay);
        Assert.Contains(bookedDayResult.Slots, slot => slot.Status == AvailabilityStatus.Booked);
    }
}
