using Rojan.Desktop.Application.BookingWorkflow;
using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Tests.BookingWorkflow;

/// <summary>
/// Phase 3B Booking Salon-Scope Migration: exercises <see cref="BookingWorkflowServicePermissionGate"/>'s
/// 3 booking write methods (Create/Cancel/Reschedule) - the backend's own
/// <c>MANAGE_BOOKINGS</c> permission is their sole authority now, not a
/// parallel check alongside legacy <c>RolePermissions</c>.
///
/// Phase 3C Customers CRM Permission Migration: <see cref="IPermissionGate"/>
/// remains a dependency of this class for <see cref="IBookingWorkflowService.CreateGuestCustomerAsync"/>
/// (Customers-scoped) - now exercised below, dual-validated against legacy
/// <see cref="Permission.CustomerEdit"/> plus the backend's own permission.
///
/// Reception Permission Contract Alignment: the backend half of that dual check now accepts
/// <c>CREATE_CUSTOMER_IDENTITY</c> as well as <c>MANAGE_CRM</c> - both are exercised below,
/// alongside the still-valid denial case where neither is present.
///
/// For every Booking-scoped test (Create/Cancel/Reschedule),
/// the <c>role</c> passed to <see cref="CreateSut"/> remains irrelevant to
/// the outcome (those 3 methods check backend permissions only), but does
/// matter for the <see cref="IBookingWorkflowService.CreateGuestCustomerAsync"/>
/// tests, which also require the legacy <see cref="Permission.CustomerEdit"/> gate.
/// </summary>
public sealed class BookingWorkflowServicePermissionGateTests
{
    private static BookingWorkflowServicePermissionGate CreateSut(IReadOnlySet<string> backendPermissions, WorkspaceRole role = WorkspaceRole.PlatformOwner) =>
        new(new StubBookingWorkflowService(), new PermissionGate(new PermissionEngine(), new StubEnterpriseContext { CurrentRole = role }),
            new BackendPermissionGate(new StubEnterpriseContext { CurrentRole = role, BackendPermissions = backendPermissions }));

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

    [Fact]
    public async Task CreateGuestCustomerAsync_SalonOwner_LegacyAndBackendBothAllow()
    {
        var sut = CreateSut(new HashSet<string> { "VIEW_CRM", "MANAGE_CRM" }, WorkspaceRole.OrganizationOwner);

        var exception = await Record.ExceptionAsync(() => sut.CreateGuestCustomerAsync("Noah Bennett", "555-0100"));

        Assert.Null(exception);
    }

    /// <summary>
    /// Still denied: <c>MANAGE_BOOKINGS</c> alone satisfies neither half of the backend's
    /// <c>EnsureBackendAny(CREATE_CUSTOMER_IDENTITY, MANAGE_CRM)</c> check. Legacy
    /// <c>RolePermissions</c> grants <see cref="WorkspaceRole.Reception"/>
    /// <see cref="Permission.CustomerEdit"/>, but that alone is never sufficient - the backend
    /// check must also pass.
    /// </summary>
    [Fact]
    public async Task CreateGuestCustomerAsync_Receptionist_DeniedWithoutCreateCustomerIdentityOrManageCrm()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_BOOKINGS" }, WorkspaceRole.Reception);

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateGuestCustomerAsync("Noah Bennett", "555-0100"));
    }

    /// <summary>
    /// Reception Permission Contract Alignment: the fix for the mismatch the test above's
    /// predecessor reported - the backend's real <c>RECEPTIONIST</c> role grants
    /// <c>CREATE_CUSTOMER_IDENTITY</c> (never <c>MANAGE_CRM</c>), and <see cref="IBackendPermissionGate.EnsureBackendAny"/>
    /// now accepts either, so Reception can reach this call once the live-verified permission is
    /// present, without gaining <c>MANAGE_CRM</c> or any other CRM capability.
    /// </summary>
    [Fact]
    public async Task CreateGuestCustomerAsync_Receptionist_AllowedWithCreateCustomerIdentity()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_BOOKINGS", "CREATE_CUSTOMER_IDENTITY" }, WorkspaceRole.Reception);

        var exception = await Record.ExceptionAsync(() => sut.CreateGuestCustomerAsync("Noah Bennett", "555-0100"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task CreateGuestCustomerAsync_Specialist_Denied()
    {
        // Legacy RolePermissions grants WorkspaceRole.Specialist CustomerRead only, never CustomerEdit.
        var sut = CreateSut(new HashSet<string> { "MANAGE_SCHEDULE_OWN" }, WorkspaceRole.Specialist);

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateGuestCustomerAsync("Noah Bennett", "555-0100"));
    }

    private static CreateBookingWorkflowRequest SampleRequest() =>
        new("customer-1", "Customer", "service-1", "Service", 30, "0", "specialist-1", "Specialist", DateTimeOffset.UtcNow.AddDays(1), string.Empty);

    private sealed class StubBookingWorkflowService : IBookingWorkflowService
    {
        public Task<BookingOptionsDto> GetBookingOptionsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new BookingOptionsDto([], [], []));

        public Task<WorkflowCustomerOptionDto> CreateGuestCustomerAsync(string fullName, string phone, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkflowCustomerOptionDto("customer-1", fullName));

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

        public WorkspaceRole CurrentRole { get; set; } = WorkspaceRole.PlatformOwner;

        public IReadOnlySet<string> BackendPermissions { get; set; } = new HashSet<string>();
    }
}
