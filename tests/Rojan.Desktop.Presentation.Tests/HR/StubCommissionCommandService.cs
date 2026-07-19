using Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

internal sealed class StubCommissionCommandService : ICommissionCommandService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<CommissionTransactionDto>>>? _generateCommissions;

    public List<CreateCommissionRuleRequest> CreateRuleRequests { get; } = [];

    public int GenerateCallCount { get; private set; }

    public StubCommissionCommandService(Func<CancellationToken, Task<IReadOnlyList<CommissionTransactionDto>>>? generateCommissions = null)
    {
        _generateCommissions = generateCommissions;
    }

    public Task<CommissionRuleDto> CreateCommissionRuleAsync(CreateCommissionRuleRequest request, CancellationToken cancellationToken = default)
    {
        CreateRuleRequests.Add(request);
        return Task.FromResult(new CommissionRuleDto("rule-new", request.EmployeeId, "Test Employee", request.Type, request.Value, request.Description));
    }

    public Task<IReadOnlyList<CommissionTransactionDto>> GenerateCommissionsFromAccountingAsync(CancellationToken cancellationToken = default)
    {
        GenerateCallCount++;
        return _generateCommissions?.Invoke(cancellationToken) ?? Task.FromResult<IReadOnlyList<CommissionTransactionDto>>([]);
    }
}
