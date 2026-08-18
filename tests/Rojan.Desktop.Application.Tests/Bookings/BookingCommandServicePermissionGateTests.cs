using Rojan.Desktop.Application.Bookings;
using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Tests.Bookings;

/// <summary>
/// Phase 3B Booking Salon-Scope Migration: exercises <see cref="BookingCommandServicePermissionGate"/> -
/// the backend's own <c>MANAGE_BOOKINGS</c> permission (see <see cref="IBackendPermissionGate"/>)
/// is now this class's sole authority, not a parallel check alongside the
/// legacy <see cref="IPermissionGate"/>/<c>RolePermissions</c> (which this
/// decorator no longer depends on at all). Scoped to <c>SALON_OWNER</c>/
/// <c>RECEPTIONIST</c> - <c>MANAGE_OWN_BOOKINGS</c>/Specialist scope is
/// deliberately excluded (see ROJAN_Phase3B_Booking_SalonScope_Migration_Report_v1.md),
/// so a Specialist session is correctly, uniformly denied here.
/// </summary>
public sealed class BookingCommandServicePermissionGateTests
{
    private static BookingCommandServicePermissionGate CreateSut(IReadOnlySet<string> backendPermissions) =>
        new(new StubBookingCommandService(), new BackendPermissionGate(new StubEnterpriseContext { BackendPermissions = backendPermissions }));

    [Fact]
    public async Task CreateBookingAsync_SalonOwner_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_SALON", "MANAGE_MEMBERSHIP", "MANAGE_CATALOG", "MANAGE_STAFF", "MANAGE_SCHEDULE_ALL", "MANAGE_SCHEDULE_OWN", "VIEW_CRM", "MANAGE_CRM", "MANAGE_BOOKINGS", "MANAGE_OWN_BOOKINGS" });

        var exception = await Record.ExceptionAsync(() => sut.CreateBookingAsync(SampleRequest()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_SalonOwner_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_BOOKINGS", "MANAGE_OWN_BOOKINGS" });

        var exception = await Record.ExceptionAsync(() => sut.UpdateBookingStatusAsync("booking-1", BookingStatus.Cancelled));

        Assert.Null(exception);
    }

    [Fact]
    public async Task RescheduleBookingAsync_SalonOwner_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_BOOKINGS", "MANAGE_OWN_BOOKINGS" });

        var exception = await Record.ExceptionAsync(() => sut.RescheduleBookingAsync("booking-1", DateTimeOffset.UtcNow.AddDays(1)));

        Assert.Null(exception);
    }

    [Fact]
    public async Task CreateBookingAsync_Receptionist_Allowed()
    {
        // Live-verified this session: the real MANAGER staff account's backend permissions include
        // MANAGE_BOOKINGS. RECEPTIONIST (per SalonRole.kt: MANAGE_BOOKINGS only) is covered identically.
        var sut = CreateSut(new HashSet<string> { "MANAGE_BOOKINGS" });

        var exception = await Record.ExceptionAsync(() => sut.CreateBookingAsync(SampleRequest()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_Receptionist_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_BOOKINGS" });

        var exception = await Record.ExceptionAsync(() => sut.UpdateBookingStatusAsync("booking-1", BookingStatus.Cancelled));

        Assert.Null(exception);
    }

    [Fact]
    public async Task CreateBookingAsync_Specialist_Denied()
    {
        // Specialist migration temporarily excluded (this phase's own scope adjustment). A real backend
        // Specialist link is granted MANAGE_SCHEDULE_OWN only (SalonPermissionResolver.kt, re-verified
        // live) - never MANAGE_BOOKINGS, never MANAGE_OWN_BOOKINGS - so this is a correct, uniform denial,
        // not a mismatch: there is only one check now, and it denies cleanly.
        var sut = CreateSut(new HashSet<string> { "MANAGE_SCHEDULE_OWN" });

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateBookingAsync(SampleRequest()));
    }

    [Fact]
    public async Task CreateBookingAsync_SpecialistWithManageOwnBookings_StillDenied()
    {
        // Confirms MANAGE_OWN_BOOKINGS is genuinely excluded from this check, not silently honored -
        // even a session that somehow had it (no real path grants it to a Specialist today) would be
        // denied, because this decorator checks only MANAGE_BOOKINGS now.
        var sut = CreateSut(new HashSet<string> { "MANAGE_OWN_BOOKINGS" });

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateBookingAsync(SampleRequest()));
    }

    [Fact]
    public async Task CreateBookingAsync_Accountant_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateBookingAsync(SampleRequest()));
    }

    [Fact]
    public async Task CreateBookingAsync_InventoryManager_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateBookingAsync(SampleRequest()));
    }

    [Fact]
    public async Task CreateBookingAsync_CustomerNoBusinessContext_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateBookingAsync(SampleRequest()));
    }

    private static CreateBookingRequest SampleRequest() =>
        new("Customer", "Service", "Specialist", DateTimeOffset.UtcNow.AddDays(1), 30, string.Empty);

    private sealed class StubBookingCommandService : IBookingCommandService
    {
        public bool SupportsInProgressAndNoShowStatuses => true;

        public Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(SampleBooking());

        public Task<BookingDto> UpdateBookingStatusAsync(string bookingId, BookingStatus status, CancellationToken cancellationToken = default) =>
            Task.FromResult(SampleBooking());

        public Task<BookingDto> RescheduleBookingAsync(string bookingId, DateTimeOffset newScheduledAt, CancellationToken cancellationToken = default) =>
            Task.FromResult(SampleBooking());

        private static BookingDto SampleBooking() =>
            new("booking-1", "customer-1", "Customer", "service-1", "Service", "specialist-1", "Specialist", DateTimeOffset.UtcNow, 30, "0", BookingStatus.Pending, string.Empty, "org-1", "branch-1");
    }

    private sealed class StubEnterpriseContext : IEnterpriseContext
    {
        public string? CurrentOrganizationId => "org-1";

        public string? CurrentBranchId => "branch-1";

        public WorkspaceRole CurrentRole => WorkspaceRole.PlatformOwner;

        public IReadOnlySet<string> BackendPermissions { get; set; } = new HashSet<string>();
    }
}
