using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Specialists;
using Rojan.Desktop.Application.Tests.Organizations;

namespace Rojan.Desktop.Application.Tests.Specialists;

/// <summary>
/// Remediation Phase 1 (RBAC Backend Authority Migration), High Priority
/// (Task 3): exercises <see cref="SpecialistCommandServicePermissionGate"/> -
/// ROJAN_Backend's own <c>MANAGE_STAFF</c> permission (see
/// <see cref="IBackendPermissionGate"/>) is now this class's sole
/// authority, not the legacy <see cref="IPermissionGate"/>/
/// <c>RolePermissions</c> (which this decorator no longer depends on at
/// all). No test file existed for this gate class before this migration -
/// this is new regression coverage, same shape
/// <c>Bookings.BookingCommandServicePermissionGateTests</c> already
/// established. Task 3's own explicit priority - "prevent Specialist
/// receiving broader permissions locally" - is <see cref="CreateSpecialistAsync_BareSpecialistLink_Denied"/>
/// and <see cref="AssignServiceAsync_BareSpecialistLink_Denied"/> below:
/// a bare Specialist-link session (MANAGE_SCHEDULE_OWN only) is denied for
/// every method here, including the two the real backend use case would
/// allow for the specialist's own record - a deliberately stricter,
/// disclosed choice (see the gate class's own doc comment).
/// </summary>
public sealed class SpecialistCommandServicePermissionGateTests
{
    private static SpecialistCommandServicePermissionGate CreateSut(IReadOnlySet<string> backendPermissions) =>
        new(new StubSpecialistCommandService(), new BackendPermissionGate(new StubEnterpriseContext { BackendPermissions = backendPermissions }));

    [Fact]
    public async Task CreateSpecialistAsync_OwnerOrManager_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_STAFF" });

        var exception = await Record.ExceptionAsync(() => sut.CreateSpecialistAsync(SampleCreateRequest()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task AssignServiceAsync_OwnerOrManager_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_STAFF" });

        var exception = await Record.ExceptionAsync(() => sut.AssignServiceAsync("specialist-1", "service-1"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task CreateSpecialistAsync_Receptionist_Denied()
    {
        // The real backend RECEPTIONIST role (SalonRole.kt) never has MANAGE_STAFF.
        var sut = CreateSut(new HashSet<string> { "MANAGE_BOOKINGS", "VIEW_CUSTOMER_IDENTITY", "CREATE_CUSTOMER_IDENTITY", "VIEW_CUSTOMER_BOOKING_HISTORY" });

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateSpecialistAsync(SampleCreateRequest()));
    }

    [Fact]
    public async Task CreateSpecialistAsync_BareSpecialistLink_Denied()
    {
        // A real backend Specialist-only relationship (SalonPermissionResolver.kt) grants
        // MANAGE_SCHEDULE_OWN alone - never MANAGE_STAFF. Locally, WorkspaceRole.Specialist never
        // had SpecialistEdit either (RolePermissions.cs), so this was already correctly denied
        // before this migration - re-asserted here as the new authority's own regression coverage.
        var sut = CreateSut(new HashSet<string> { "MANAGE_SCHEDULE_OWN" });

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateSpecialistAsync(SampleCreateRequest()));
    }

    [Fact]
    public async Task AssignServiceAsync_BareSpecialistLink_Denied()
    {
        // ROJAN_Backend's own SpecialistServiceUseCases allows MANAGE_STAFF OR the specialist
        // acting on their own record (MANAGE_SCHEDULE_OWN). This gate deliberately checks only
        // MANAGE_STAFF (see the gate class's own doc comment) - stricter than the backend's own
        // use case, not wider, per this task's explicit "prevent broader permissions" priority.
        var sut = CreateSut(new HashSet<string> { "MANAGE_SCHEDULE_OWN" });

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.AssignServiceAsync("specialist-1", "service-1"));
    }

    [Fact]
    public async Task CreateSpecialistAsync_NoBackendPermissions_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateSpecialistAsync(SampleCreateRequest()));
    }

    private static CreateSpecialistRequest SampleCreateRequest() =>
        new("Ava Carter", "Senior Stylist", "ava@example.com", "555-0101", "10 years of experience");

    private sealed class StubSpecialistCommandService : ISpecialistCommandService
    {
        public Task<SpecialistDto> CreateSpecialistAsync(CreateSpecialistRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(SampleSpecialist());

        public Task<SpecialistDto> UpdateSpecialistAsync(UpdateSpecialistRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(SampleSpecialist());

        public Task<SpecialistSkillDto> AddSkillAsync(string specialistId, string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SpecialistSkillDto("skill-1", specialistId, name));

        public Task RemoveSkillAsync(string specialistId, string skillId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AssignServiceAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveServiceAssignmentAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        private static SpecialistDto SampleSpecialist() =>
            new("specialist-1", "Ava Carter", "Senior Stylist", "ava@example.com", "555-0101", SpecialistStatus.Active, "10 years of experience");
    }
}
