using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.HR;

/// <summary>Default <see cref="ICommissionQueryService"/> implementation.</summary>
public sealed class CommissionQueryService : ICommissionQueryService
{
    private readonly DomainHr.IHrRepository _repository;

    public CommissionQueryService(DomainHr.IHrRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CommissionRuleDto>> GetCommissionRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _repository.GetCommissionRulesAsync(cancellationToken).ConfigureAwait(true);
        return rules.Select(HrMapper.MapCommissionRule).ToList();
    }

    public async Task<IReadOnlyList<CommissionTransactionDto>> GetAllCommissionTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var transactions = await _repository.GetCommissionTransactionsAsync(cancellationToken).ConfigureAwait(true);
        return transactions.OrderByDescending(t => t.EarnedAt).Select(HrMapper.MapCommissionTransaction).ToList();
    }

    public async Task<IReadOnlyList<CommissionTransactionDto>> GetCommissionHistoryForEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        var transactions = await _repository.GetCommissionTransactionsAsync(cancellationToken).ConfigureAwait(true);
        return transactions.Where(t => t.EmployeeId == employeeId).OrderByDescending(t => t.EarnedAt).Select(HrMapper.MapCommissionTransaction).ToList();
    }

    public async Task<decimal> GetMonthlyCommissionTotalAsync(string employeeId, int month, int year, CancellationToken cancellationToken = default)
    {
        var transactions = await _repository.GetCommissionTransactionsAsync(cancellationToken).ConfigureAwait(true);
        return transactions
            .Where(t => t.EmployeeId == employeeId && t.EarnedAt.Month == month && t.EarnedAt.Year == year)
            .Sum(t => t.CommissionAmount);
    }
}
