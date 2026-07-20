using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Accounting;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Every method requires <see cref="Permission.AccountingManage"/>.</summary>
public sealed class PaymentCommandServicePermissionGate : IPaymentCommandService
{
    private readonly IPaymentCommandService _inner;
    private readonly IPermissionGate _permissionGate;

    public PaymentCommandServicePermissionGate(IPaymentCommandService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<PaymentDto> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.AccountingManage);
        return _inner.RecordPaymentAsync(request, cancellationToken);
    }

    public Task<CashSessionDto> OpenCashSessionAsync(string cashierName, decimal openingFloat, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.AccountingManage);
        return _inner.OpenCashSessionAsync(cashierName, openingFloat, cancellationToken);
    }

    public Task<CashSessionDto> CloseCashSessionAsync(string sessionId, decimal closingBalance, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.AccountingManage);
        return _inner.CloseCashSessionAsync(sessionId, closingBalance, cancellationToken);
    }
}
