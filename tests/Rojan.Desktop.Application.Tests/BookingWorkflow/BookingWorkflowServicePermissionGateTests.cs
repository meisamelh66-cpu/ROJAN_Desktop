using Rojan.Desktop.Application.BookingWorkflow;
using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Tests.BookingWorkflow;

/// <summary>
/// Phase 3B Booking Salon-Scope Migration: exercises <see cref="BookingWorkflowServicePermissionGate"/>'s
/// 3 booking write methods (Create/Cancel/Reschedule) - the backend's own
/// <c>MANAGE_BOOKINGS</c> permission is their sole authority now, not a
/// parallel check alongside legacy <c>RolePermissions</c>. <see cref="IPermissionGate"/>
/// remains a dependency of this class only for <see cref="IBookingWorkflowService.CreateGuestCustomerAsync"/>
/// (Customers-scoped, out of this phase's scope, not exercised here) - the
/// <c>role</c> passed to <see cref="CreateSut"/> is therefore irrelevant to
/// every test below except by way of constructing that unused-here
/// dependency.
/// </summary>
public sealed class BookingWorkflowServicePermissionGateTests
{
    private static BookingWorkflowServicePermissionGate CreateSut(IReadOnlySet<string> backendPermissions) =>
        new(new StubBookingWorkflowService(), new PermissionGate(new PermissionEngine(), new StubEnterpriseContext()),
            new BackendPermissionGate(new StubEnterpriseContext { BackendPermissions = backendPermissions }));

    [Fact]
    public async Task CreateBookingAsync_SalonOwner_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_BOOKINGS", "MANAGE_OWN_BOOKINGS" });

        var exception = await Record.ExceptionAsync(() => sut.CreateBookingAsync(SampleRequest()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task CancelBookingAsync_Receptionist_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_BOOKINGS" });

        var exception = await Record.ExceptionAsync(() => sut.CancelBookingAsync("booking-1"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task RescheduleBookingAsync_Specialist_Denied()
    {
        // Specialist migration temporarily excluded - MANAGE_SCHEDULE_OWN (the real backend grant for a
        // Specialist link) does not satisfy the MANAGE_BOOKINGS-only check.
        var sut = CreateSut(new HashSet<string> { "MANAGE_SCHEDULE_OWN" });

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.RescheduleBookingAsync("booking-1", DateTimeOffset.UtcNow.AddDays(1)));
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

    [Fact]
    public async Task GetBookingOptionsAsync_NeverGated_AlwaysReachesInner()
    {
        // Picker reads stay open to anyone who can reach the Bookings module - unchanged by this phase.
        var sut = CreateSut(new HashSet<string>());

        var exception = await Record.ExceptionAsync(() => sut.GetBookingOptionsAsync());

        Assert.Null(exception);
    }

    private static CreateBookingWorkflowRequest SampleRequest() =>
        new("customer-1", "Customer", "service-1", "Service", 30, "0", "specialist-1", "Specialist", DateTimeOffset.UtcNow.AddDays(1), string.Empty);

    private sealed class StubBookingWorkflowService : IBookingWorkflowService
    {
        public Task<BookingOptionsDto> GetBookingOptionsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new BookingOptionsDto([], [], []));

        public Task<WorkflowCustomerOptionDto> CreateGuestCustomerAsync(string fullName, string phone, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Out of Phase 3B scope - Customers, not Bookings.");

        public Task<IReadOnlyList<WorkflowSlotDto>> GetAvailableSlotsAsync(string specialistId, string serviceId, DateOnly scheduleDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkflowSlotDto>>([]);

        public Task<BookingConfirmationDto> CreateBookingAsync(CreateBookingWorkflowRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new BookingConfirmationDto("booking-1", "Customer", "Service", "Specialist", DateTimeOffset.UtcNow.AddDays(1), 30, "0"));

        public Task CancelBookingAsync(string bookingId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<BookingConfirmationDto> RescheduleBookingAsync(string bookingId, DateTimeOffset newSlotStart, CancellationToken cancellationToken = default) =>
            Task.FromResult(new BookingConfirmationDto("booking-1", "Customer", "Service", "Specialist", newSlotStart, 30, "0"));
    }

    private sealed class StubEnterpriseContext : IEnterpriseContext
    {
        public string? CurrentOrganizationId => "org-1";

        public string? CurrentBranchId => "branch-1";

        public WorkspaceRole CurrentRole => WorkspaceRole.PlatformOwner;

        public IReadOnlySet<string> BackendPermissions { get; set; } = new HashSet<string>();
    }
}
