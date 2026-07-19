namespace Rojan.Desktop.Application.HR;

/// <summary>Read-only use cases Presentation depends on to load Payroll summaries.</summary>
public interface IPayrollQueryService
{
    public Task<IReadOnlyList<PayrollSummaryDto>> GetPayrollSummariesAsync(CancellationToken cancellationToken = default);

    public Task<PayrollSummaryDto?> GetPayrollSummaryForEmployeeAsync(string employeeId, int month, int year, CancellationToken cancellationToken = default);
}
