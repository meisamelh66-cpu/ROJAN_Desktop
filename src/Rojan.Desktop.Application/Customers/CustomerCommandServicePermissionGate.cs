using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Phase 22A: Enterprise Context Migration. Wraps the real
/// <see cref="CustomerCommandService"/> with permission enforcement -
/// registered as <see cref="ICustomerCommandService"/> in place of the
/// raw service (see <c>DependencyInjection.ServiceCollectionExtensions</c>),
/// so every Presentation caller is gated automatically without either the
/// wrapped service or its existing unit tests needing to know this layer
/// exists.
///
/// Remediation Phase 1 (RBAC Backend Authority Migration): migrated off
/// the legacy <see cref="IPermissionGate"/>/<c>RolePermissions</c> check
/// entirely, same shape <c>Bookings.BookingCommandServicePermissionGate</c>
/// already established - <see cref="IBackendPermissionGate"/>'s
/// <c>MANAGE_CRM</c> check is now this class's sole authority. Every
/// method here calls the Infrastructure-layer backend customer
/// repository's real <c>POST/PATCH /customers</c>/<c>/notes</c>/<c>/tags</c> endpoints
/// (never the identity-only <c>/customers/identity</c> endpoint), which
/// ROJAN_Backend's own <c>CreateCustomerUseCase</c>/<c>UpdateCustomerUseCase</c>/
/// <c>AddCustomerNoteUseCase</c>/<c>AddCustomerTagUseCase</c>/
/// <c>RemoveCustomerTagUseCase</c> all gate on <c>Permission.MANAGE_CRM</c>
/// alone (verified directly against ROJAN_Backend source this migration) -
/// so one permission string covers every method here, same pattern as
/// Booking's single <c>MANAGE_BOOKINGS</c> check.
///
/// Deliberate, disclosed behavior change (see
/// ROJAN_DESKTOP_RBAC_PHASE1_IMPLEMENTATION_REPORT_v1.md's own Security
/// Impact section for the full account): the legacy local check granted
/// <c>Permission.CustomerEdit</c> to <c>WorkspaceRole.Reception</c>, which
/// <c>SalonSessionAdapter.ToWorkspaceRole</c> maps *any* non-owner,
/// non-"MANAGER" backend session to - including a bare Specialist link.
/// The real backend RECEPTIONIST role, and a bare Specialist link, both
/// lack <c>MANAGE_CRM</c> (only MANAGER/Owner have it) - so this migration
/// intentionally narrows Reception/Specialist-mapped sessions from "can
/// create/edit customers, add notes/tags" to "cannot," matching real
/// backend authority. This is the fix, not an accidental regression.
/// </summary>
public sealed class CustomerCommandServicePermissionGate : ICustomerCommandService
{
    private const string ManageCrm = "MANAGE_CRM";

    private readonly ICustomerCommandService _inner;
    private readonly IBackendPermissionGate _backendPermissionGate;

    public CustomerCommandServicePermissionGate(ICustomerCommandService inner, IBackendPermissionGate backendPermissionGate)
    {
        _inner = inner;
        _backendPermissionGate = backendPermissionGate;
    }

    public Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageCrm);
        return _inner.CreateCustomerAsync(request, cancellationToken);
    }

    public Task<CustomerDto> UpdateCustomerAsync(UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageCrm);
        return _inner.UpdateCustomerAsync(request, cancellationToken);
    }

    public Task<CustomerNoteDto> AddNoteAsync(string customerId, string text, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageCrm);
        return _inner.AddNoteAsync(customerId, text, cancellationToken);
    }

    public Task<CustomerTagDto> AddTagAsync(string customerId, string label, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageCrm);
        return _inner.AddTagAsync(customerId, label, cancellationToken);
    }

    public Task RemoveTagAsync(string customerId, string tagId, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageCrm);
        return _inner.RemoveTagAsync(customerId, tagId, cancellationToken);
    }
}
