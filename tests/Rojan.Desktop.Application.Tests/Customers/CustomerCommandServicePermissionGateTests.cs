using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Tests.Organizations;

namespace Rojan.Desktop.Application.Tests.Customers;

/// <summary>
/// Remediation Phase 1 (RBAC Backend Authority Migration): exercises
/// <see cref="CustomerCommandServicePermissionGate"/> - ROJAN_Backend's own
/// <c>MANAGE_CRM</c> permission (see <see cref="IBackendPermissionGate"/>)
/// is now this class's sole authority, not the legacy
/// <see cref="IPermissionGate"/>/<c>RolePermissions</c> (which this
/// decorator no longer depends on at all) - same shape
/// <c>Bookings.BookingCommandServicePermissionGateTests</c> already
/// established for its own migration.
/// </summary>
public sealed class CustomerCommandServicePermissionGateTests
{
    private static CustomerCommandServicePermissionGate CreateSut(IReadOnlySet<string> backendPermissions, StubCustomerRepository repository) =>
        new(
            new CustomerCommandService(repository, new StubEnterpriseContext()),
            new BackendPermissionGate(new StubEnterpriseContext { BackendPermissions = backendPermissions }));

    [Fact]
    public async Task CreateCustomerAsync_OwnerOrManager_Allowed()
    {
        var repository = new StubCustomerRepository();
        var sut = CreateSut(new HashSet<string> { "MANAGE_CRM" }, repository);
        var request = new CreateCustomerRequest("Noah Bennett", string.Empty, "noah@example.com", "555-0100", string.Empty);

        var created = await sut.CreateCustomerAsync(request);

        Assert.Equal("Noah Bennett", created.FullName);
        Assert.Single(repository.Customers);
    }

    [Fact]
    public async Task AddNoteAsync_OwnerOrManager_Allowed()
    {
        var repository = new StubCustomerRepository();
        var sut = CreateSut(new HashSet<string> { "MANAGE_CRM" }, repository);

        var exception = await Record.ExceptionAsync(() => sut.AddNoteAsync("customer-1", "Prefers quiet appointments"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task AddTagAsync_OwnerOrManager_Allowed()
    {
        var repository = new StubCustomerRepository();
        var sut = CreateSut(new HashSet<string> { "MANAGE_CRM" }, repository);

        var exception = await Record.ExceptionAsync(() => sut.AddTagAsync("customer-1", "VIP"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task CreateCustomerAsync_Receptionist_ThrowsAndNeverCreatesCustomer()
    {
        // Deliberate, disclosed behavior change (ROJAN_DESKTOP_RBAC_PHASE1_IMPLEMENTATION_REPORT_v1.md's
        // own Security Impact section): the real backend RECEPTIONIST role (SalonRole.kt) never has
        // MANAGE_CRM - only MANAGE_BOOKINGS, VIEW_CUSTOMER_IDENTITY, CREATE_CUSTOMER_IDENTITY,
        // VIEW_CUSTOMER_BOOKING_HISTORY. This correctly denies what the legacy local check
        // (WorkspaceRole.Reception -> Permission.CustomerEdit) used to allow.
        var repository = new StubCustomerRepository();
        var sut = CreateSut(new HashSet<string> { "MANAGE_BOOKINGS", "VIEW_CUSTOMER_IDENTITY", "CREATE_CUSTOMER_IDENTITY", "VIEW_CUSTOMER_BOOKING_HISTORY" }, repository);
        var request = new CreateCustomerRequest("Noah Bennett", string.Empty, "noah@example.com", "555-0100", string.Empty);

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateCustomerAsync(request));

        Assert.Empty(repository.Customers);
    }

    [Fact]
    public async Task CreateCustomerAsync_BareSpecialistLink_Denied()
    {
        // A real backend Specialist-only relationship (SalonPermissionResolver.kt) grants
        // MANAGE_SCHEDULE_OWN alone - never MANAGE_CRM. RBAC Migration Map's own Gap 3: this
        // session type is mapped locally to WorkspaceRole.Reception by SalonSessionAdapter, which
        // is exactly the over-grant this migration closes for the Customers module.
        var repository = new StubCustomerRepository();
        var sut = CreateSut(new HashSet<string> { "MANAGE_SCHEDULE_OWN" }, repository);
        var request = new CreateCustomerRequest("Noah Bennett", string.Empty, "noah@example.com", "555-0100", string.Empty);

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateCustomerAsync(request));

        Assert.Empty(repository.Customers);
    }

    [Fact]
    public async Task CreateCustomerAsync_NoBackendPermissions_Denied()
    {
        var repository = new StubCustomerRepository();
        var sut = CreateSut(new HashSet<string>(), repository);
        var request = new CreateCustomerRequest("Noah Bennett", string.Empty, "noah@example.com", "555-0100", string.Empty);

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateCustomerAsync(request));

        Assert.Empty(repository.Customers);
    }
}
