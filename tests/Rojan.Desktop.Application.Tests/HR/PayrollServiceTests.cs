using Rojan.Desktop.Application.HR;
using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.Tests.HR;

public sealed class PayrollServiceTests
{
    private static DomainHr.Employee MakeEmployee(string id, string fullName, decimal baseSalary) =>
        new(id, string.Empty, fullName, $"{id}@rojan.example", "+1 555", DomainHr.EmployeeRole.Colorist, DomainHr.Department.Hair, DomainHr.EmploymentType.FullTime, DomainHr.EmployeeStatus.Active, new DateOnly(2022, 1, 1), baseSalary);

    [Fact]
    public async Task GetPayrollSummaryForEmployeeAsync_NoMatch_ReturnsNull()
    {
        var repository = new StubHrRepository();
        var sut = new PayrollQueryService(repository);

        var result = await sut.GetPayrollSummaryForEmployeeAsync("employee-1", 7, 2026);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPayrollSummaryForEmployeeAsync_Match_ReturnsSummary()
    {
        var repository = new StubHrRepository();
        repository.PayrollSummaries.Add(new DomainHr.PayrollSummary("payroll-1", "employee-1", "Jordan Lee", 7, 2026, 3200m, 450m, 100m, 50m, 3700m, DateTimeOffset.Now));
        var sut = new PayrollQueryService(repository);

        var result = await sut.GetPayrollSummaryForEmployeeAsync("employee-1", 7, 2026);

        Assert.NotNull(result);
        Assert.Equal(3700m, result.NetSalary);
    }

    [Fact]
    public async Task GeneratePayrollSummaryAsync_UnknownEmployee_ThrowsInvalidOperationException()
    {
        var repository = new StubHrRepository();
        var sut = new PayrollCommandService(repository);
        var request = new GeneratePayrollRequest("no-such-employee", 7, 2026, 0m, 0m);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GeneratePayrollSummaryAsync(request));
    }

    [Fact]
    public async Task GeneratePayrollSummaryAsync_SumsCommissionsForThatMonthOnly()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "Jordan Lee", 3200m));
        var july = new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);
        var june = new DateTimeOffset(2026, 6, 15, 9, 0, 0, TimeSpan.Zero);
        repository.CommissionTransactions.Add(new DomainHr.CommissionTransaction("commission-1", "employee-1", "Jordan Lee", "invoice-1", "Haircut", 100m, 15m, july));
        repository.CommissionTransactions.Add(new DomainHr.CommissionTransaction("commission-2", "employee-1", "Jordan Lee", "invoice-2", "Colour", 200m, 30m, june));
        var sut = new PayrollCommandService(repository);
        var request = new GeneratePayrollRequest("employee-1", 7, 2026, 100m, 50m);

        var result = await sut.GeneratePayrollSummaryAsync(request);

        Assert.Equal(15m, result.CommissionTotal);
        Assert.Equal(3265m, result.NetSalary);
        Assert.Single(repository.PayrollSummaries);
    }
}
