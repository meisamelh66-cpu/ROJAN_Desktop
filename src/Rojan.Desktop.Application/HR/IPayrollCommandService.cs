namespace Rojan.Desktop.Application.HR;

/// <summary>Write use cases for Payroll - the monthly summary generator.</summary>
public interface IPayrollCommandService
{
    /// <summary>Sums the employee's <see cref="CommissionTransactionDto"/>s for the given month/year and combines them with Base Salary + Bonus - Deduction via <c>Domain.HR.PayrollCalculator</c> to produce a <see cref="PayrollSummaryDto"/>.</summary>
    public Task<PayrollSummaryDto> GeneratePayrollSummaryAsync(GeneratePayrollRequest request, CancellationToken cancellationToken = default);
}
