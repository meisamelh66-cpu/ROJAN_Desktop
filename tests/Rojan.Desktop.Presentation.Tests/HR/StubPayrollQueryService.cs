using Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

internal sealed class StubPayrollQueryService : IPayrollQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<PayrollSummaryDto>>>? _getSummaries;

    public StubPayrollQueryService(Func<CancellationToken, Task<IReadOnlyList<PayrollSummaryDto>>>? getSummaries = null)
    {
        _getSummaries = getSummaries;
    }

    public Task<IReadOnlyList<PayrollSummaryDto>> GetPayrollSummariesAsync(CancellationToken cancellationToken = default) =>
        _getSummaries?.Invoke(cancellationToken) ?? Task.FromResult<IReadOnlyList<PayrollSummaryDto>>([]);

    public Task<PayrollSummaryDto?> GetPayrollSummaryForEmployeeAsync(string employeeId, int month, int year, CancellationToken cancellationToken = default) =>
        Task.FromResult<PayrollSummaryDto?>(null);
}
