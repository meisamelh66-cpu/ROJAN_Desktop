using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.HR;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Requires <see cref="Permission.HrManage"/>.</summary>
public sealed class PayrollCommandServicePermissionGate : IPayrollCommandService
{
    private readonly IPayrollCommandService _inner;
    private readonly IPermissionGate _permissionGate;

    public PayrollCommandServicePermissionGate(IPayrollCommandService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<PayrollSummaryDto> GeneratePayrollSummaryAsync(GeneratePayrollRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.HrManage);
        return _inner.GeneratePayrollSummaryAsync(request, cancellationToken);
    }
}
