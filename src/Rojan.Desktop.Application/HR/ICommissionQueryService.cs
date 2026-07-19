namespace Rojan.Desktop.Application.HR;

/// <summary>Read-only use cases Presentation depends on to load Commission rules and history.</summary>
public interface ICommissionQueryService
{
    public Task<IReadOnlyList<CommissionRuleDto>> GetCommissionRulesAsync(CancellationToken cancellationToken = default);

    /// <summary>Every commission transaction across every employee - backs the Commission section's history list.</summary>
    public Task<IReadOnlyList<CommissionTransactionDto>> GetAllCommissionTransactionsAsync(CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<CommissionTransactionDto>> GetCommissionHistoryForEmployeeAsync(string employeeId, CancellationToken cancellationToken = default);

    public Task<decimal> GetMonthlyCommissionTotalAsync(string employeeId, int month, int year, CancellationToken cancellationToken = default);
}
