using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.HR;

/// <summary>Default <see cref="IPayrollCommandService"/> implementation - depends on <see cref="DomainHr.IHrRepository"/> directly (within-slice, not the sibling <see cref="ICommissionQueryService"/>), same convention as <c>Accounting.PaymentCommandService</c> depending on <c>IAccountingRepository</c> directly.</summary>
public sealed class PayrollCommandService : IPayrollCommandService
{
    private readonly DomainHr.IHrRepository _repository;

    public PayrollCommandService(DomainHr.IHrRepository repository)
    {
        _repository = repository;
    }

    public async Task<PayrollSummaryDto> GeneratePayrollSummaryAsync(GeneratePayrollRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetEmployeeByIdAsync(request.EmployeeId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Employee '{request.EmployeeId}' was not found.");

        var transactions = await _repository.GetCommissionTransactionsAsync(cancellationToken).ConfigureAwait(true);
        var commissionTotal = transactions
            .Where(t => t.EmployeeId == request.EmployeeId && t.EarnedAt.Month == request.Month && t.EarnedAt.Year == request.Year)
            .Sum(t => t.CommissionAmount);

        var netSalary = DomainHr.PayrollCalculator.ComputeNetSalary(employee.BaseSalary, commissionTotal, request.Bonus, request.Deduction);

        var summary = new DomainHr.PayrollSummary(
            Guid.NewGuid().ToString(), employee.Id, employee.FullName, request.Month, request.Year,
            employee.BaseSalary, commissionTotal, request.Bonus, request.Deduction, netSalary, DateTimeOffset.Now);

        var created = await _repository.CreatePayrollSummaryAsync(summary, cancellationToken).ConfigureAwait(true);
        return HrMapper.MapPayrollSummary(created);
    }
}
