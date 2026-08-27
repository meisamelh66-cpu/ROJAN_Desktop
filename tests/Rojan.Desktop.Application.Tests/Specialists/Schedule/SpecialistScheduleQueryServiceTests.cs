using Rojan.Desktop.Application.Specialists.Schedule;
using DomainSchedule = Rojan.Desktop.Domain.Specialists.Schedule;

namespace Rojan.Desktop.Application.Tests.Specialists.Schedule;

/// <summary>Exercises <see cref="SpecialistScheduleQueryService"/> - Domain-&gt;Application mapping for all four resource groups, and the empty-result ("nothing configured yet") case.</summary>
public sealed class SpecialistScheduleQueryServiceTests
{
    [Fact]
    public async Task GetWeeklyAvailabilityAsync_MapsEveryField()
    {
        var repository = new StubSpecialistScheduleRepository
        {
            WeeklyAvailability = [new DomainSchedule.WeeklyAvailability("wa-1", "specialist-1", DayOfWeek.Monday, [new DomainSchedule.TimeInterval(TimeSpan.FromHours(9), TimeSpan.FromHours(13))])],
        };
        var sut = new SpecialistScheduleQueryService(repository);

        var availability = Assert.Single(await sut.GetWeeklyAvailabilityAsync("specialist-1"));

        Assert.Equal(DayOfWeek.Monday, availability.DayOfWeek);
        Assert.Equal(TimeSpan.FromHours(9), availability.Intervals[0].Start);
    }

    [Fact]
    public async Task GetWeeklyAvailabilityAsync_NothingConfigured_ReturnsEmptyList_NotAnError()
    {
        var sut = new SpecialistScheduleQueryService(new StubSpecialistScheduleRepository());

        var availability = await sut.GetWeeklyAvailabilityAsync("specialist-1");

        Assert.Empty(availability);
    }

    [Fact]
    public async Task GetOverridesAsync_RedactedReason_PassesThroughAsNull()
    {
        var repository = new StubSpecialistScheduleRepository
        {
            Overrides = [new DomainSchedule.ScheduleOverride("ov-1", "specialist-1", new DateOnly(2026, 9, 1), [], Reason: null)],
        };
        var sut = new SpecialistScheduleQueryService(repository);

        var @override = Assert.Single(await sut.GetOverridesAsync("specialist-1"));

        Assert.Null(@override.Reason);
        Assert.Empty(@override.Intervals);
    }

    [Fact]
    public async Task GetLeaveAsync_MapsEveryField()
    {
        var repository = new StubSpecialistScheduleRepository
        {
            Leave = [new DomainSchedule.SpecialistLeave("lv-1", "specialist-1", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 7), "Vacation")],
        };
        var sut = new SpecialistScheduleQueryService(repository);

        var leave = Assert.Single(await sut.GetLeaveAsync("specialist-1"));

        Assert.Equal("Vacation", leave.Reason);
    }

    [Fact]
    public async Task GetBlocksAsync_MapsEveryField()
    {
        var repository = new StubSpecialistScheduleRepository
        {
            Blocks = [new DomainSchedule.SpecialistBlock("bl-1", "specialist-1", new DateOnly(2026, 9, 1), new DomainSchedule.TimeInterval(TimeSpan.FromHours(14), TimeSpan.FromHours(15)), "Dentist")],
        };
        var sut = new SpecialistScheduleQueryService(repository);

        var block = Assert.Single(await sut.GetBlocksAsync("specialist-1"));

        Assert.Equal(TimeSpan.FromHours(14), block.Interval.Start);
        Assert.Equal("Dentist", block.Reason);
    }
}
