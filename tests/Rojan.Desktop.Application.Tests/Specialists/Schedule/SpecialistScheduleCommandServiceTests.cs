using Rojan.Desktop.Application.Specialists.Schedule;

namespace Rojan.Desktop.Application.Tests.Specialists.Schedule;

/// <summary>Exercises <see cref="SpecialistScheduleCommandService"/> - thin passthrough to the repository plus DTO&lt;-&gt;Domain mapping, for every one of the eight mutation methods.</summary>
public sealed class SpecialistScheduleCommandServiceTests
{
    [Fact]
    public async Task SetWeeklyAvailabilityAsync_PassesThroughToRepository()
    {
        var repository = new StubSpecialistScheduleRepository();
        var sut = new SpecialistScheduleCommandService(repository);
        var intervals = new List<TimeIntervalDto> { new(TimeSpan.FromHours(9), TimeSpan.FromHours(13)) };

        await sut.SetWeeklyAvailabilityAsync("specialist-1", DayOfWeek.Monday, intervals);

        Assert.Equal(("specialist-1", DayOfWeek.Monday), (repository.LastSetWeeklyAvailabilityCall!.Value.SpecialistId, repository.LastSetWeeklyAvailabilityCall!.Value.DayOfWeek));
    }

    [Fact]
    public async Task RemoveWeeklyAvailabilityAsync_PassesThroughToRepository()
    {
        var repository = new StubSpecialistScheduleRepository();
        var sut = new SpecialistScheduleCommandService(repository);

        await sut.RemoveWeeklyAvailabilityAsync("specialist-1", DayOfWeek.Monday);

        Assert.Equal(("specialist-1", DayOfWeek.Monday), repository.LastRemoveWeeklyAvailabilityCall);
    }

    [Fact]
    public async Task SetOverrideAsync_PassesReasonAndEmptyIntervalsThrough()
    {
        var repository = new StubSpecialistScheduleRepository();
        var sut = new SpecialistScheduleCommandService(repository);

        var result = await sut.SetOverrideAsync("specialist-1", new DateOnly(2026, 9, 1), [], "Holiday");

        Assert.Empty(result.Intervals);
        Assert.Equal("Holiday", result.Reason);
        Assert.Equal("Holiday", repository.LastSetOverrideCall!.Value.Reason);
    }

    [Fact]
    public async Task RemoveOverrideAsync_PassesThroughToRepository()
    {
        var repository = new StubSpecialistScheduleRepository();
        var sut = new SpecialistScheduleCommandService(repository);

        await sut.RemoveOverrideAsync("specialist-1", "ov-1");

        Assert.Equal(("specialist-1", "ov-1"), repository.LastRemoveOverrideCall);
    }

    [Fact]
    public async Task CreateLeaveAsync_PassesThroughToRepository()
    {
        var repository = new StubSpecialistScheduleRepository();
        var sut = new SpecialistScheduleCommandService(repository);

        var result = await sut.CreateLeaveAsync("specialist-1", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 7), "Vacation");

        Assert.Equal("Vacation", result.Reason);
    }

    [Fact]
    public async Task RemoveLeaveAsync_PassesThroughToRepository()
    {
        var repository = new StubSpecialistScheduleRepository();
        var sut = new SpecialistScheduleCommandService(repository);

        await sut.RemoveLeaveAsync("specialist-1", "lv-1");

        Assert.Equal(("specialist-1", "lv-1"), repository.LastRemoveLeaveCall);
    }

    [Fact]
    public async Task CreateBlockAsync_PassesThroughToRepository()
    {
        var repository = new StubSpecialistScheduleRepository();
        var sut = new SpecialistScheduleCommandService(repository);
        var interval = new TimeIntervalDto(TimeSpan.FromHours(14), TimeSpan.FromHours(15));

        var result = await sut.CreateBlockAsync("specialist-1", new DateOnly(2026, 9, 1), interval, "Dentist");

        Assert.Equal(TimeSpan.FromHours(14), result.Interval.Start);
        Assert.Equal("Dentist", result.Reason);
    }

    [Fact]
    public async Task RemoveBlockAsync_PassesThroughToRepository()
    {
        var repository = new StubSpecialistScheduleRepository();
        var sut = new SpecialistScheduleCommandService(repository);

        await sut.RemoveBlockAsync("specialist-1", "bl-1");

        Assert.Equal(("specialist-1", "bl-1"), repository.LastRemoveBlockCall);
    }
}
