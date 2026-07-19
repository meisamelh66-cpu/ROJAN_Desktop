using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.HR;

/// <summary>Default <see cref="IPayrollQueryService"/> implementation.</summary>
public sealed class PayrollQueryService : IPayrollQueryService
{
    private readonly DomainHr.IHrRepository _repository;

    public PayrollQueryService(DomainHr.IHrRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PayrollSummaryDto>> GetPayrollSummariesAsync(CancellationToken cancellationToken = default)
    {
        var summaries = await _repository.GetPayrollSummariesAsync(cancellationToken).ConfigureAwait(true);
        return summaries.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).Select(HrMapper.MapPayrollSummary).ToList();
    }

    public async Task<PayrollSummaryDto?> GetPayrollSummaryForEmployeeAsync(string employeeId, int month, int year, CancellationToken cancellationToken = default)
    {
        var summaries = await _repository.GetPayrollSummariesAsync(cancellationToken).ConfigureAwait(true);
        var match = summaries.FirstOrDefault(s => s.EmployeeId == employeeId && s.Month == month && s.Year == year);
        return match is null ? null : HrMapper.MapPayrollSummary(match);
    }
}
