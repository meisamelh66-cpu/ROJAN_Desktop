using Rojan.Desktop.Application.Calendar;
using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Tests.Calendar;

/// <summary>
/// Phase 3B Booking Salon-Scope Migration: exercises <see cref="CalendarCommandServicePermissionGate"/> -
/// same reasoning and persona coverage as <c>Bookings.BookingCommandServicePermissionGateTests</c>,
/// scoped to slot reserve/release instead of booking CRUD. Legacy
/// <see cref="IPermissionGate"/> is no longer a dependency of this class at
/// all.
/// </summary>
public sealed class CalendarCommandServicePermissionGateTests
{
    private static CalendarCommandServicePermissionGate CreateSut(IReadOnlySet<string> backendPermissions) =>
        new(new StubCalendarCommandService(), new BackendPermissionGate(new StubEnterpriseContext { BackendPermissions = backendPermissions }));

    [Fact]
    public async Task ReserveSlotAsync_SalonOwner_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_BOOKINGS", "MANAGE_OWN_BOOKINGS" });

        var exception = await Record.ExceptionAsync(() => sut.ReserveSlotAsync("specialist-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30)));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ReserveSlotAsync_Receptionist_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_BOOKINGS" });

        var exception = await Record.ExceptionAsync(() => sut.ReserveSlotAsync("specialist-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30)));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ReleaseSlotAsync_Specialist_Denied()
    {
        // Specialist migration temporarily excluded - a real backend Specialist link's
        // MANAGE_SCHEDULE_OWN-only grant does not satisfy this check.
        var sut = CreateSut(new HashSet<string> { "MANAGE_SCHEDULE_OWN" });

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.ReleaseSlotAsync("specialist-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30)));
    }

    [Fact]
    public async Task ReserveSlotAsync_Accountant_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.ReserveSlotAsync("specialist-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30)));
    }

    [Fact]
    public async Task ReserveSlotAsync_InventoryManager_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.ReserveSlotAsync("specialist-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30)));
    }

    [Fact]
    public async Task ReserveSlotAsync_CustomerNoBusinessContext_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.ReserveSlotAsync("specialist-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30)));
    }

    private sealed class StubCalendarCommandService : ICalendarCommandService
    {
        public Task ReserveSlotAsync(string specialistId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ReleaseSlotAsync(string specialistId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubEnterpriseContext : IEnterpriseContext
    {
        public string? CurrentOrganizationId => "org-1";

        public string? CurrentBranchId => "branch-1";

        public WorkspaceRole CurrentRole => WorkspaceRole.PlatformOwner;

        public IReadOnlySet<string> BackendPermissions { get; set; } = new HashSet<string>();
    }
}
