using Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

internal sealed class StubPayrollCommandService : IPayrollCommandService
{
    public List<GeneratePayrollRequest> GenerateRequests { get; } = [];

    public Task<PayrollSummaryDto> GeneratePayrollSummaryAsync(GeneratePayrollRequest request, CancellationToken cancellationToken = default)
    {
        GenerateRequests.Add(request);
        var netSalary = 2500m + 0m + request.Bonus - request.Deduction;
        return Task.FromResult(new PayrollSummaryDto("payroll-new", request.EmployeeId, "Test Employee", request.Month, request.Year, 2500m, 0m, request.Bonus, request.Deduction, netSalary, DateTimeOffset.Now));
    }
}
