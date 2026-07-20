using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Accounting;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Every method requires <see cref="Permission.AccountingManage"/>.</summary>
public sealed class InvoiceCommandServicePermissionGate : IInvoiceCommandService
{
    private readonly IInvoiceCommandService _inner;
    private readonly IPermissionGate _permissionGate;

    public InvoiceCommandServicePermissionGate(IInvoiceCommandService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.AccountingManage);
        return _inner.CreateInvoiceAsync(request, cancellationToken);
    }

    public Task<InvoiceDto> CancelInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.AccountingManage);
        return _inner.CancelInvoiceAsync(invoiceId, cancellationToken);
    }
}
