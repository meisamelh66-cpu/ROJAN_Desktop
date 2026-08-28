using Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

internal sealed class StubPayrollCommandService : IPayrollCommandService
{
    public List<GeneratePayrollRequest> GenerateRequests { get; } = [];

    /// <summary>Production Hardening (missing-guard sweep, Wave B): when set, GeneratePayrollSummaryAsync throws this instead of succeeding. Same seam pattern as Customers.StubCustomerCommandService.CreateCustomerException. The call is still recorded before the throw.</summary>
    public Exception? GeneratePayrollException { get; set; }

    public Task<PayrollSummaryDto> GeneratePayrollSummaryAsync(GeneratePayrollRequest request, CancellationToken cancellationToken = default)
    {
        GenerateRequests.Add(request);
        if (GeneratePayrollException is not null)
        {
            return Task.FromException<PayrollSummaryDto>(GeneratePayrollException);
        }

        var netSalary = 2500m + 0m + request.Bonus - request.Deduction;
        return Task.FromResult(new PayrollSummaryDto("payroll-new", request.EmployeeId, "Test Employee", request.Month, request.Year, 2500m, 0m, request.Bonus, request.Deduction, netSalary, DateTimeOffset.Now));
    }
}
