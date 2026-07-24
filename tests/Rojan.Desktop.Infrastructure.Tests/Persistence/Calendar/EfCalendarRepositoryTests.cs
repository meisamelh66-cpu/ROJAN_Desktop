using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Rojan.Desktop.Infrastructure.Persistence;
using Rojan.Desktop.Infrastructure.Persistence.Calendar;
using DomainCalendar = Rojan.Desktop.Domain.Calendar;

namespace Rojan.Desktop.Infrastructure.Tests.Persistence.Calendar;

/// <summary>
/// Exercises <see cref="EfCalendarRepository"/> against a real, migrated,
/// temp-file SQLite database - never the production
/// <see cref="SqlitePersistenceOptions.Default"/> path. Same shape as
/// <c>Customers.EfCustomerRepositoryTests</c>/<c>Services.EfServiceRepositoryTests</c>/
/// <c>Bookings.EfBookingRepositoryTests</c>.
///
/// Like Services, <see cref="DomainCalendar.ICalendarRepository"/> has no
/// create/update-schedule method at all (see <see cref="EfCalendarRepository"/>'s
/// own doc comment), so every test that needs a
/// <see cref="DomainCalendar.WorkingSchedule"/> seeds its
/// <see cref="WorkingScheduleEntity"/>/<see cref="WorkingScheduleBreakEntity"/>
/// rows directly through a <see cref="RojanDbContext"/> (bypassing the
/// repository entirely for arrange, exactly mirroring how the real
/// database will only ever be populated) and then exercises only the
/// methods the contract actually has.
/// </summary>
public sealed class EfCalendarRepositoryTests : IDisposable
{
    private readonly string _testRoot;
    private readonly TestDbContextFactory _contextFactory;
    private readonly EfCalendarRepository _sut;

    public EfCalendarRepositoryTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"));
        var options = new SqlitePersistenceOptions(Path.Combine(_testRoot, "rojan.db"));
        var optionsBuilder = new DbContextOptionsBuilder<RojanDbContext>().UseSqlite(options.ConnectionString);
        _contextFactory = new TestDbContextFactory(optionsBuilder.Options);

        // Applies the real Sprint 6 Commit 6 migration (not EnsureCreated,
        // which would create the schema straight from the model and never
        // actually exercise the migration file itself).
        using var context = _contextFactory.CreateDbContext();
        context.Database.Migrate();

        _sut = new EfCalendarRepository(_contextFactory);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private static DomainCalendar.WorkingSchedule MakeSchedule(
        string id = "schedule-1",
        string specialistId = "specialist-1",
        DayOfWeek dayOfWeek = DayOfWeek.Monday,
        IReadOnlyList<DomainCalendar.TimeSlot>? breaks = null) =>
        new(id, specialistId, "Jordan Lee", dayOfWeek, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), breaks);

    /// <summary>Arrange-only seeding, bypassing the repository entirely - see this class's own doc comment for why (no create method exists on the contract).</summary>
    private async Task SeedScheduleAsync(DomainCalendar.WorkingSchedule schedule)
    {
        await using var context = _contextFactory.CreateDbContext();
        context.WorkingSchedules.Add(CalendarEntityMapper.MapToEntity(schedule));
        foreach (var brk in schedule.Breaks)
        {
            context.WorkingScheduleBreaks.Add(CalendarEntityMapper.MapBreakToEntity(schedule.Id, brk));
        }

        await context.SaveChangesAsync();
    }

    // ----- Availability (WorkingSchedule) persistence -----

    [Fact]
    public async Task GetWorkingSchedulesAsync_NoSchedules_ReturnsEmptyList()
    {
        var schedules = await _sut.GetWorkingSchedulesAsync();

        Assert.Empty(schedules);
    }

    [Fact]
    public async Task GetWorkingSchedulesAsync_ReturnsTheSeededScheduleWithNoBreaks()
    {
        var schedule = MakeSchedule();
        await SeedScheduleAsync(schedule);

        var schedules = await _sut.GetWorkingSchedulesAsync();

        var found = Assert.Single(schedules);
        Assert.Equal(schedule.Id, found.Id);
        Assert.Equal(schedule.SpecialistId, found.SpecialistId);
        Assert.Equal(schedule.SpecialistName, found.SpecialistName);
        Assert.Equal(schedule.DayOfWeek, found.DayOfWeek);
        Assert.Equal(schedule.StartTime, found.StartTime);
        Assert.Equal(schedule.EndTime, found.EndTime);
        Assert.Empty(found.Breaks);
    }

    [Fact]
    public async Task GetWorkingSchedulesAsync_ReturnsEverySeededSchedule()
    {
        await SeedScheduleAsync(MakeSchedule("schedule-1", dayOfWeek: DayOfWeek.Monday));
        await SeedScheduleAsync(MakeSchedule("schedule-2", dayOfWeek: DayOfWeek.Tuesday));

        var schedules = await _sut.GetWorkingSchedulesAsync();

        Assert.Equal(2, schedules.Count);
        Assert.Contains(schedules, s => s.Id == "schedule-1");
        Assert.Contains(schedules, s => s.Id == "schedule-2");
    }

    [Fact]
    public async Task GetWorkingSchedulesAsync_MultipleSpecialists_EachScheduleKeepsItsOwnSpecialistId()
    {
        await SeedScheduleAsync(MakeSchedule("schedule-1", specialistId: "specialist-1"));
        await SeedScheduleAsync(MakeSchedule("schedule-2", specialistId: "specialist-2"));

        var schedules = await _sut.GetWorkingSchedulesAsync();

        Assert.Equal("specialist-1", schedules.Single(s => s.Id == "schedule-1").SpecialistId);
        Assert.Equal("specialist-2", schedules.Single(s => s.Id == "schedule-2").SpecialistId);
    }

    // ----- Break (slot) persistence -----

    [Fact]
    public async Task GetWorkingSchedulesAsync_SeededBreak_RoundTripsExactly()
    {
        var now = DateTimeOffset.Now;
        var lunchBreak = new DomainCalendar.TimeSlot(now, now.AddMinutes(30));
        await SeedScheduleAsync(MakeSchedule(breaks: [lunchBreak]));

        var schedules = await _sut.GetWorkingSchedulesAsync();

        var found = Assert.Single(Assert.Single(schedules).Breaks);
        Assert.Equal(lunchBreak.Start, found.Start);
        Assert.Equal(lunchBreak.End, found.End);
    }

    [Fact]
    public async Task GetWorkingSchedulesAsync_MultipleBreaksOnOneSchedule_ReturnsEveryOne()
    {
        var now = DateTimeOffset.Now;
        var breaks = new List<DomainCalendar.TimeSlot>
        {
            new(now, now.AddMinutes(30)),
            new(now.AddHours(3), now.AddHours(3).AddMinutes(15)),
        };
        await SeedScheduleAsync(MakeSchedule(breaks: breaks));

        var schedules = await _sut.GetWorkingSchedulesAsync();

        Assert.Equal(2, Assert.Single(schedules).Breaks.Count);
    }

    [Fact]
    public async Task GetWorkingSchedulesAsync_BreaksOnlyAttachToTheirOwnSchedule()
    {
        var now = DateTimeOffset.Now;
        await SeedScheduleAsync(MakeSchedule("schedule-1", breaks: [new DomainCalendar.TimeSlot(now, now.AddMinutes(30))]));
        await SeedScheduleAsync(MakeSchedule("schedule-2", breaks: []));

        var schedules = await _sut.GetWorkingSchedulesAsync();

        Assert.Single(schedules.Single(s => s.Id == "schedule-1").Breaks);
        Assert.Empty(schedules.Single(s => s.Id == "schedule-2").Breaks);
    }

    // ----- Reserve/release (booked slot) persistence -----

    [Fact]
    public async Task GetBookedSlotsAsync_NoReservations_ReturnsEmptyList()
    {
        var slots = await _sut.GetBookedSlotsAsync("specialist-1", DateOnly.FromDateTime(DateTime.Today));

        Assert.Empty(slots);
    }

    [Fact]
    public async Task ReserveSlotAsync_ThenGetBookedSlotsAsync_ReturnsThePersistedSlot()
    {
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var start = new DateTimeOffset(date.Year, date.Month, date.Day, 10, 0, 0, TimeSpan.Zero);
        var slot = new DomainCalendar.TimeSlot(start, start.AddMinutes(30));

        await _sut.ReserveSlotAsync("specialist-1", slot);
        var slots = await _sut.GetBookedSlotsAsync("specialist-1", date);

        var found = Assert.Single(slots);
        Assert.Equal(slot.Start, found.Start);
        Assert.Equal(slot.End, found.End);
    }

    [Fact]
    public async Task ReleaseSlotAsync_RemovesThePersistedReservation()
    {
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var start = new DateTimeOffset(date.Year, date.Month, date.Day, 10, 0, 0, TimeSpan.Zero);
        var slot = new DomainCalendar.TimeSlot(start, start.AddMinutes(30));
        await _sut.ReserveSlotAsync("specialist-1", slot);

        await _sut.ReleaseSlotAsync("specialist-1", slot);
        var slots = await _sut.GetBookedSlotsAsync("specialist-1", date);

        Assert.Empty(slots);
    }

    [Fact]
    public async Task ReleaseSlotAsync_ReservationDoesNotExist_DoesNotThrow()
    {
        var start = DateTimeOffset.Now.AddDays(1);
        var slot = new DomainCalendar.TimeSlot(start, start.AddMinutes(30));

        var exception = await Record.ExceptionAsync(() => _sut.ReleaseSlotAsync("specialist-1", slot));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ReserveSlotAsync_OverlappingSlotForSameSpecialist_DoesNotThrow()
    {
        // Conflict detection is Application's job
        // (Application.Calendar.CalendarCommandService.ReserveSlotAsync
        // checks GetBookedSlotsAsync for an overlap before ever calling
        // this repository) - the repository itself, like
        // FakeCalendarRepository, blindly persists whatever it is given,
        // the same "dumb data access, Application owns the rules"
        // contract every Ef*Repository in this app follows.
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var start = new DateTimeOffset(date.Year, date.Month, date.Day, 10, 0, 0, TimeSpan.Zero);
        await _sut.ReserveSlotAsync("specialist-1", new DomainCalendar.TimeSlot(start, start.AddMinutes(30)));

        var overlapping = new DomainCalendar.TimeSlot(start.AddMinutes(15), start.AddMinutes(45));
        var exception = await Record.ExceptionAsync(() => _sut.ReserveSlotAsync("specialist-1", overlapping));
        var slots = await _sut.GetBookedSlotsAsync("specialist-1", date);

        Assert.Null(exception);
        Assert.Equal(2, slots.Count);
    }

    // ----- Date filtering -----

    [Fact]
    public async Task GetBookedSlotsAsync_OnlyReturnsSlotsOnTheRequestedDate()
    {
        var today = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var tomorrow = today.AddDays(1);
        var todayStart = new DateTimeOffset(today.Year, today.Month, today.Day, 10, 0, 0, TimeSpan.Zero);
        var tomorrowStart = new DateTimeOffset(tomorrow.Year, tomorrow.Month, tomorrow.Day, 10, 0, 0, TimeSpan.Zero);
        await _sut.ReserveSlotAsync("specialist-1", new DomainCalendar.TimeSlot(todayStart, todayStart.AddMinutes(30)));
        await _sut.ReserveSlotAsync("specialist-1", new DomainCalendar.TimeSlot(tomorrowStart, tomorrowStart.AddMinutes(30)));

        var slots = await _sut.GetBookedSlotsAsync("specialist-1", today);

        var found = Assert.Single(slots);
        Assert.Equal(todayStart, found.Start);
    }

    // ----- Isolation across specialists -----

    [Fact]
    public async Task GetBookedSlotsAsync_OnlyReturnsSlotsForTheRequestedSpecialist()
    {
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var start = new DateTimeOffset(date.Year, date.Month, date.Day, 10, 0, 0, TimeSpan.Zero);
        await _sut.ReserveSlotAsync("specialist-1", new DomainCalendar.TimeSlot(start, start.AddMinutes(30)));
        await _sut.ReserveSlotAsync("specialist-2", new DomainCalendar.TimeSlot(start, start.AddMinutes(30)));

        var slots = await _sut.GetBookedSlotsAsync("specialist-1", date);

        Assert.Single(slots);
    }

    [Fact]
    public async Task ReleaseSlotAsync_OnlyAffectsTheMatchingSpecialistNeverAnotherOne()
    {
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var start = new DateTimeOffset(date.Year, date.Month, date.Day, 10, 0, 0, TimeSpan.Zero);
        var slot = new DomainCalendar.TimeSlot(start, start.AddMinutes(30));
        await _sut.ReserveSlotAsync("specialist-1", slot);
        await _sut.ReserveSlotAsync("specialist-2", slot);

        await _sut.ReleaseSlotAsync("specialist-1", slot);

        Assert.Empty(await _sut.GetBookedSlotsAsync("specialist-1", date));
        Assert.Single(await _sut.GetBookedSlotsAsync("specialist-2", date));
    }

    /// <summary>Minimal <see cref="IDbContextFactory{TContext}"/> for tests - hands out a fresh <see cref="RojanDbContext"/> per call against the same temp-file connection string, same shape <see cref="Rojan.Desktop.Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure"/> registers in the running app. Also used directly by this test class's own seeding helper, since the repository itself has no create method for a working schedule.</summary>
    private sealed class TestDbContextFactory : IDbContextFactory<RojanDbContext>
    {
        private readonly DbContextOptions<RojanDbContext> _options;

        public TestDbContextFactory(DbContextOptions<RojanDbContext> options)
        {
            _options = options;
        }

        public RojanDbContext CreateDbContext() => new(_options);
    }
}
