using Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

internal sealed class StubCommissionCommandService : ICommissionCommandService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<CommissionTransactionDto>>>? _generateCommissions;

    public List<CreateCommissionRuleRequest> CreateRuleRequests { get; } = [];

    public int GenerateCallCount { get; private set; }

    /// <summary>Production Hardening (missing-guard sweep, Wave B): when set, CreateCommissionRuleAsync throws this instead of succeeding. Same seam pattern as Customers.StubCustomerCommandService.CreateCustomerException. GenerateCommissionsFromAccountingAsync failures use the existing constructor delegate. The call is still recorded before the throw.</summary>
    public Exception? CreateCommissionRuleException { get; set; }

    public StubCommissionCommandService(Func<CancellationToken, Task<IReadOnlyList<CommissionTransactionDto>>>? generateCommissions = null)
    {
        _generateCommissions = generateCommissions;
    }

    public Task<CommissionRuleDto> CreateCommissionRuleAsync(CreateCommissionRuleRequest request, CancellationToken cancellationToken = default)
    {
        CreateRuleRequests.Add(request);
        return CreateCommissionRuleException is not null
            ? Task.FromException<CommissionRuleDto>(CreateCommissionRuleException)
            : Task.FromResult(new CommissionRuleDto("rule-new", request.EmployeeId, "Test Employee", request.Type, request.Value, request.Description));
    }

    public Task<IReadOnlyList<CommissionTransactionDto>> GenerateCommissionsFromAccountingAsync(CancellationToken cancellationToken = default)
    {
        GenerateCallCount++;
        return _generateCommissions?.Invoke(cancellationToken) ?? Task.FromResult<IReadOnlyList<CommissionTransactionDto>>([]);
    }
}
