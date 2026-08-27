using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Services;
using Rojan.Desktop.Application.Tests.Organizations;

namespace Rojan.Desktop.Application.Tests.Services;

/// <summary>
/// Remediation Phase 1 (RBAC Backend Authority Migration): exercises
/// <see cref="ServiceCommandServicePermissionGate"/> - ROJAN_Backend's own
/// <c>MANAGE_CATALOG</c> permission (see <see cref="IBackendPermissionGate"/>)
/// is now this class's sole authority, not the legacy
/// <see cref="IPermissionGate"/>/<c>RolePermissions</c> (which this
/// decorator no longer depends on at all). No test file existed for this
/// gate class before this migration - this is new regression coverage,
/// same shape <c>Bookings.BookingCommandServicePermissionGateTests</c>
/// already established.
/// </summary>
public sealed class ServiceCommandServicePermissionGateTests
{
    private static ServiceCommandServicePermissionGate CreateSut(IReadOnlySet<string> backendPermissions) =>
        new(new StubServiceCommandService(), new BackendPermissionGate(new StubEnterpriseContext { BackendPermissions = backendPermissions }));

    [Fact]
    public async Task CreateServiceAsync_OwnerOrManager_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_CATALOG" });

        var exception = await Record.ExceptionAsync(() => sut.CreateServiceAsync(SampleCreateRequest()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task UpdateServiceAsync_OwnerOrManager_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_CATALOG" });

        var exception = await Record.ExceptionAsync(() => sut.UpdateServiceAsync(SampleUpdateRequest()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task CreateServiceAsync_Receptionist_Denied()
    {
        // The real backend RECEPTIONIST role (SalonRole.kt) never has MANAGE_CATALOG.
        var sut = CreateSut(new HashSet<string> { "MANAGE_BOOKINGS", "VIEW_CUSTOMER_IDENTITY", "CREATE_CUSTOMER_IDENTITY", "VIEW_CUSTOMER_BOOKING_HISTORY" });

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateServiceAsync(SampleCreateRequest()));
    }

    [Fact]
    public async Task CreateServiceAsync_BareSpecialistLink_Denied()
    {
        // A real backend Specialist-only relationship (SalonPermissionResolver.kt) grants
        // MANAGE_SCHEDULE_OWN alone - never MANAGE_CATALOG.
        var sut = CreateSut(new HashSet<string> { "MANAGE_SCHEDULE_OWN" });

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateServiceAsync(SampleCreateRequest()));
    }

    [Fact]
    public async Task CreateServiceAsync_NoBackendPermissions_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateServiceAsync(SampleCreateRequest()));
    }

    private static CreateServiceRequest SampleCreateRequest() =>
        new("category-1", "Haircut", "Classic cut", 30, 250000m);

    private static UpdateServiceRequest SampleUpdateRequest() =>
        new("service-1", "category-1", "Haircut", "Classic cut", 30, 250000m, ServiceStatus.Active);

    private sealed class StubServiceCommandService : IServiceCommandService
    {
        public Task<AssignedSpecialistDto> AssignSpecialistAsync(string serviceId, string specialistName, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AssignedSpecialistDto("assignment-1", serviceId, "specialist-1", specialistName));

        public Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(SampleService());

        public Task<ServiceDto> UpdateServiceAsync(UpdateServiceRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(SampleService());

        private static ServiceDto SampleService() =>
            new("service-1", "Haircut", ServiceCategory.Hair, ServiceStatus.Active, 30, "250,000 تومان", "Classic cut");
    }
}
