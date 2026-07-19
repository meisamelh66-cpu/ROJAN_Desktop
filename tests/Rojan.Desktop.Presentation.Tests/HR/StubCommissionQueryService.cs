using Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

internal sealed class StubCommissionQueryService : ICommissionQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<CommissionRuleDto>>>? _getRules;
    private readonly Func<CancellationToken, Task<IReadOnlyList<CommissionTransactionDto>>>? _getAllTransactions;

    public StubCommissionQueryService(
        Func<CancellationToken, Task<IReadOnlyList<CommissionRuleDto>>>? getRules = null,
        Func<CancellationToken, Task<IReadOnlyList<CommissionTransactionDto>>>? getAllTransactions = null)
    {
        _getRules = getRules;
        _getAllTransactions = getAllTransactions;
    }

    public Task<IReadOnlyList<CommissionRuleDto>> GetCommissionRulesAsync(CancellationToken cancellationToken = default) =>
        _getRules?.Invoke(cancellationToken) ?? Task.FromResult<IReadOnlyList<CommissionRuleDto>>([]);

    public Task<IReadOnlyList<CommissionTransactionDto>> GetAllCommissionTransactionsAsync(CancellationToken cancellationToken = default) =>
        _getAllTransactions?.Invoke(cancellationToken) ?? Task.FromResult<IReadOnlyList<CommissionTransactionDto>>([]);

    public Task<IReadOnlyList<CommissionTransactionDto>> GetCommissionHistoryForEmployeeAsync(string employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommissionTransactionDto>>([]);

    public Task<decimal> GetMonthlyCommissionTotalAsync(string employeeId, int month, int year, CancellationToken cancellationToken = default) =>
        Task.FromResult(0m);
}
