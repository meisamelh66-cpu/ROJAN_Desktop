using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Rojan.Desktop.Infrastructure.Persistence;
using Rojan.Desktop.Infrastructure.Persistence.Bookings;
using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Infrastructure.Tests.Persistence.Bookings;

/// <summary>
/// Exercises <see cref="EfBookingRepository"/> against a real, migrated,
/// temp-file SQLite database - never the production
/// <see cref="SqlitePersistenceOptions.Default"/> path. Same shape as
/// <c>Customers.EfCustomerRepositoryTests</c>/<c>Specialists.EfSpecialistRepositoryTests</c>/
/// <c>Services.EfServiceRepositoryTests</c>.
/// </summary>
public sealed class EfBookingRepositoryTests : IDisposable
{
    private readonly string _testRoot;
    private readonly EfBookingRepository _sut;

    public EfBookingRepositoryTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"));
        var options = new SqlitePersistenceOptions(Path.Combine(_testRoot, "rojan.db"));
        var optionsBuilder = new DbContextOptionsBuilder<RojanDbContext>().UseSqlite(options.ConnectionString);
        var contextFactory = new TestDbContextFactory(optionsBuilder.Options);

        // Applies the real Sprint 6 Commit 5 migration (not EnsureCreated,
        // which would create the schema straight from the model and never
        // actually exercise the migration file itself).
        using var context = contextFactory.CreateDbContext();
        context.Database.Migrate();

        _sut = new EfBookingRepository(contextFactory);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private static DomainBookings.Booking MakeBooking(
        string id = "booking-1",
        string customerId = "customer-1",
        string specialistId = "specialist-1",
        string serviceId = "service-1",
        DateTimeOffset? scheduledAt = null,
        DomainBookings.BookingStatus status = DomainBookings.BookingStatus.Pending,
        string organizationId = "org-1",
        string branchId = "branch-1") =>
        new(id, customerId, "Amelia Hart", serviceId, "Haircut", specialistId, "Jordan Lee",
            scheduledAt ?? DateTimeOffset.Now.AddDays(3), 60, "80", status, "Notes", organizationId, branchId);

    [Fact]
    public async Task CreateBookingAsync_ThenGetBookingByIdAsync_ReturnsThePersistedBooking()
    {
        var booking = MakeBooking();

        await _sut.CreateBookingAsync(booking);
        var found = await _sut.GetBookingByIdAsync("booking-1");

        Assert.NotNull(found);
        Assert.Equal(booking, found);
    }

    [Fact]
    public async Task GetBookingByIdAsync_NoMatchingBooking_ReturnsNull()
    {
        var found = await _sut.GetBookingByIdAsync("missing-booking");

        Assert.Null(found);
    }

    [Fact]
    public async Task GetBookingsAsync_NoBookings_ReturnsEmptyList()
    {
        var bookings = await _sut.GetBookingsAsync();

        Assert.Empty(bookings);
    }

    [Fact]
    public async Task GetBookingsAsync_ReturnsEveryPersistedBooking()
    {
        await _sut.CreateBookingAsync(MakeBooking("booking-1"));
        await _sut.CreateBookingAsync(MakeBooking("booking-2"));

        var bookings = await _sut.GetBookingsAsync();

        Assert.Equal(2, bookings.Count);
        Assert.Contains(bookings, booking => booking.Id == "booking-1");
        Assert.Contains(bookings, booking => booking.Id == "booking-2");
    }

    [Fact]
    public async Task GetBookingsAsync_BookingsAcrossMultipleOrganizationsAndBranches_ReturnsAllUnfiltered()
    {
        // Repository-level contract: no Organization/Branch filtering
        // happens here - that stays
        // Application.Bookings.BookingQueryService.ScopeToCurrentSession's
        // job, same reasoning Customers.EfCustomerRepositoryTests'
        // equivalent test already establishes.
        await _sut.CreateBookingAsync(MakeBooking("booking-1", organizationId: "org-1", branchId: "branch-1"));
        await _sut.CreateBookingAsync(MakeBooking("booking-2", organizationId: "org-1", branchId: "branch-2"));
        await _sut.CreateBookingAsync(MakeBooking("booking-3", organizationId: "org-2", branchId: "branch-3"));

        var bookings = await _sut.GetBookingsAsync();

        Assert.Equal(3, bookings.Count);
        Assert.Contains(bookings, b => b.OrganizationId == "org-1" && b.BranchId == "branch-1");
        Assert.Contains(bookings, b => b.OrganizationId == "org-1" && b.BranchId == "branch-2");
        Assert.Contains(bookings, b => b.OrganizationId == "org-2" && b.BranchId == "branch-3");
    }

    [Theory]
    [InlineData(DomainBookings.BookingStatus.Pending)]
    [InlineData(DomainBookings.BookingStatus.Confirmed)]
    [InlineData(DomainBookings.BookingStatus.InProgress)]
    [InlineData(DomainBookings.BookingStatus.Completed)]
    [InlineData(DomainBookings.BookingStatus.Cancelled)]
    [InlineData(DomainBookings.BookingStatus.NoShow)]
    public async Task UpdateBookingStatusAsync_EveryStatus_PersistsTheNewStatus(DomainBookings.BookingStatus newStatus)
    {
        await _sut.CreateBookingAsync(MakeBooking(status: DomainBookings.BookingStatus.Pending));

        await _sut.UpdateBookingStatusAsync("booking-1", newStatus);
        var found = await _sut.GetBookingByIdAsync("booking-1");

        Assert.Equal(newStatus, found?.Status);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_OnlyChangesStatusNeverAnyOtherField()
    {
        var original = MakeBooking(status: DomainBookings.BookingStatus.Pending);
        await _sut.CreateBookingAsync(original);

        var updated = await _sut.UpdateBookingStatusAsync("booking-1", DomainBookings.BookingStatus.Confirmed);

        Assert.Equal(DomainBookings.BookingStatus.Confirmed, updated.Status);
        Assert.Equal(original.CustomerId, updated.CustomerId);
        Assert.Equal(original.CustomerName, updated.CustomerName);
        Assert.Equal(original.ServiceId, updated.ServiceId);
        Assert.Equal(original.SpecialistId, updated.SpecialistId);
        Assert.Equal(original.ScheduledAt, updated.ScheduledAt);
        Assert.Equal(original.Price, updated.Price);
        Assert.Equal(original.Notes, updated.Notes);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_BookingDoesNotExist_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateBookingStatusAsync("missing-booking", DomainBookings.BookingStatus.Confirmed));
    }

    [Fact]
    public async Task RescheduleBookingAsync_PersistsTheNewScheduledAt()
    {
        var original = MakeBooking(scheduledAt: DateTimeOffset.Now.AddDays(1));
        await _sut.CreateBookingAsync(original);
        var newScheduledAt = DateTimeOffset.Now.AddDays(10);

        await _sut.RescheduleBookingAsync("booking-1", newScheduledAt);
        var found = await _sut.GetBookingByIdAsync("booking-1");

        Assert.Equal(newScheduledAt, found?.ScheduledAt);
    }

    [Fact]
    public async Task RescheduleBookingAsync_OnlyChangesScheduledAtNeverAnyOtherField()
    {
        var original = MakeBooking(status: DomainBookings.BookingStatus.Confirmed);
        await _sut.CreateBookingAsync(original);
        var newScheduledAt = DateTimeOffset.Now.AddDays(20);

        var updated = await _sut.RescheduleBookingAsync("booking-1", newScheduledAt);

        Assert.Equal(newScheduledAt, updated.ScheduledAt);
        Assert.Equal(original.Status, updated.Status);
        Assert.Equal(original.CustomerId, updated.CustomerId);
        Assert.Equal(original.SpecialistId, updated.SpecialistId);
        Assert.Equal(original.ServiceId, updated.ServiceId);
    }

    [Fact]
    public async Task RescheduleBookingAsync_BookingDoesNotExist_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RescheduleBookingAsync("missing-booking", DateTimeOffset.Now.AddDays(1)));
    }

    [Fact]
    public async Task CreateBookingAsync_CustomerIdAndCustomerNameRoundTripExactly()
    {
        await _sut.CreateBookingAsync(MakeBooking(customerId: "customer-42"));

        var found = await _sut.GetBookingByIdAsync("booking-1");

        Assert.Equal("customer-42", found?.CustomerId);
        Assert.Equal("Amelia Hart", found?.CustomerName);
    }

    [Fact]
    public async Task CreateBookingAsync_SpecialistIdAndSpecialistNameRoundTripExactly()
    {
        await _sut.CreateBookingAsync(MakeBooking(specialistId: "specialist-77"));

        var found = await _sut.GetBookingByIdAsync("booking-1");

        Assert.Equal("specialist-77", found?.SpecialistId);
        Assert.Equal("Jordan Lee", found?.SpecialistName);
    }

    [Fact]
    public async Task CreateBookingAsync_ServiceIdAndServiceNameRoundTripExactly()
    {
        await _sut.CreateBookingAsync(MakeBooking(serviceId: "service-99"));

        var found = await _sut.GetBookingByIdAsync("booking-1");

        Assert.Equal("service-99", found?.ServiceId);
        Assert.Equal("Haircut", found?.ServiceName);
    }

    [Fact]
    public async Task CreateBookingAsync_EmptyCustomerAndServiceIds_PersistsWithoutThrowing()
    {
        // Invalid/missing relationship behavior: CustomerId/ServiceId/
        // SpecialistId are plain, unconstrained text columns (no foreign
        // keys to Customers/Specialists/Services - see BookingEntity's own
        // doc comment), so an empty reference must persist exactly like
        // FakeBookingRepository's own seed data already has (every seeded
        // booking has an empty CustomerId; booking-3 also has an empty
        // ServiceId) - never a constraint violation.
        var booking = MakeBooking(customerId: string.Empty, serviceId: string.Empty);

        var exception = await Record.ExceptionAsync(() => _sut.CreateBookingAsync(booking));
        var found = await _sut.GetBookingByIdAsync("booking-1");

        Assert.Null(exception);
        Assert.Equal(string.Empty, found?.CustomerId);
        Assert.Equal(string.Empty, found?.ServiceId);
    }

    [Fact]
    public async Task CreateBookingAsync_ReferencingANonExistentCustomerSpecialistOrService_PersistsWithoutThrowing()
    {
        // Same "no foreign key, so no referential integrity is enforced or
        // even possible" contract, from the other direction: an id that
        // simply does not exist anywhere else must be accepted exactly
        // like an empty one is.
        var booking = MakeBooking(customerId: "does-not-exist", specialistId: "does-not-exist", serviceId: "does-not-exist");

        var exception = await Record.ExceptionAsync(() => _sut.CreateBookingAsync(booking));

        Assert.Null(exception);
    }

    /// <summary>Minimal <see cref="IDbContextFactory{TContext}"/> for tests - hands out a fresh <see cref="RojanDbContext"/> per call against the same temp-file connection string, same shape <see cref="Rojan.Desktop.Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure"/> registers in the running app.</summary>
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
