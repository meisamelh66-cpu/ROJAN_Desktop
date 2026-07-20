using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Phase 22A: Enterprise Context Migration. Wraps the real
/// <see cref="CustomerCommandService"/> with permission enforcement -
/// registered as <see cref="ICustomerCommandService"/> in place of the
/// raw service (see <c>DependencyInjection.ServiceCollectionExtensions</c>),
/// so every Presentation caller is gated automatically without either the
/// wrapped service or its existing unit tests needing to know this layer
/// exists. Every method requires <see cref="Permission.CustomerEdit"/> -
/// the same permission Presentation's own navigation/command-enablement
/// already keys off for the Customers module.
/// </summary>
public sealed class CustomerCommandServicePermissionGate : ICustomerCommandService
{
    private readonly ICustomerCommandService _inner;
    private readonly IPermissionGate _permissionGate;

    public CustomerCommandServicePermissionGate(ICustomerCommandService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.CustomerEdit);
        return _inner.CreateCustomerAsync(request, cancellationToken);
    }

    public Task<CustomerDto> UpdateCustomerAsync(UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.CustomerEdit);
        return _inner.UpdateCustomerAsync(request, cancellationToken);
    }

    public Task<CustomerNoteDto> AddNoteAsync(string customerId, string text, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.CustomerEdit);
        return _inner.AddNoteAsync(customerId, text, cancellationToken);
    }

    public Task<CustomerTagDto> AddTagAsync(string customerId, string label, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.CustomerEdit);
        return _inner.AddTagAsync(customerId, label, cancellationToken);
    }

    public Task RemoveTagAsync(string customerId, string tagId, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.CustomerEdit);
        return _inner.RemoveTagAsync(customerId, tagId, cancellationToken);
    }
}
