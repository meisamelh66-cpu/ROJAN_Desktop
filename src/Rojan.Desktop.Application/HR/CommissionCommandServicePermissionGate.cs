using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.HR;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Every method requires <see cref="Permission.HrManage"/>.</summary>
public sealed class CommissionCommandServicePermissionGate : ICommissionCommandService
{
    private readonly ICommissionCommandService _inner;
    private readonly IPermissionGate _permissionGate;

    public CommissionCommandServicePermissionGate(ICommissionCommandService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<CommissionRuleDto> CreateCommissionRuleAsync(CreateCommissionRuleRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.HrManage);
        return _inner.CreateCommissionRuleAsync(request, cancellationToken);
    }

    public Task<IReadOnlyList<CommissionTransactionDto>> GenerateCommissionsFromAccountingAsync(CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.HrManage);
        return _inner.GenerateCommissionsFromAccountingAsync(cancellationToken);
    }
}
