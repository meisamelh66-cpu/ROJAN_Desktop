using Rojan.Desktop.Application.HR;
using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.Tests.HR;

public sealed class CommissionQueryServiceTests
{
    [Fact]
    public async Task GetCommissionHistoryForEmployeeAsync_ReturnsOnlyThatEmployeesTransactions()
    {
        var repository = new StubHrRepository();
        repository.CommissionTransactions.Add(new DomainHr.CommissionTransaction("commission-1", "employee-1", "Jordan Lee", "invoice-1", "Haircut", 65m, 6.5m, DateTimeOffset.Now));
        repository.CommissionTransactions.Add(new DomainHr.CommissionTransaction("commission-2", "employee-2", "Priya Nair", "invoice-2", "Manicure", 40m, 4m, DateTimeOffset.Now));
        var sut = new CommissionQueryService(repository);

        var result = await sut.GetCommissionHistoryForEmployeeAsync("employee-1");

        var transaction = Assert.Single(result);
        Assert.Equal("commission-1", transaction.Id);
    }

    [Fact]
    public async Task GetMonthlyCommissionTotalAsync_SumsOnlyMatchingMonthAndYear()
    {
        var repository = new StubHrRepository();
        var now = DateTimeOffset.Now;
        repository.CommissionTransactions.Add(new DomainHr.CommissionTransaction("commission-1", "employee-1", "Jordan Lee", "invoice-1", "Haircut", 65m, 6.5m, now));
        repository.CommissionTransactions.Add(new DomainHr.CommissionTransaction("commission-2", "employee-1", "Jordan Lee", "invoice-2", "Colour", 150m, 22.5m, now.AddMonths(-1)));
        var sut = new CommissionQueryService(repository);

        var total = await sut.GetMonthlyCommissionTotalAsync("employee-1", now.Month, now.Year);

        Assert.Equal(6.5m, total);
    }

    [Fact]
    public async Task GetAllCommissionTransactionsAsync_ReturnsEveryTransactionAcrossEmployees()
    {
        var repository = new StubHrRepository();
        repository.CommissionTransactions.Add(new DomainHr.CommissionTransaction("commission-1", "employee-1", "Jordan Lee", "invoice-1", "Haircut", 65m, 6.5m, DateTimeOffset.Now));
        repository.CommissionTransactions.Add(new DomainHr.CommissionTransaction("commission-2", "employee-2", "Priya Nair", "invoice-2", "Manicure", 40m, 4m, DateTimeOffset.Now));
        var sut = new CommissionQueryService(repository);

        var result = await sut.GetAllCommissionTransactionsAsync();

        Assert.Equal(2, result.Count);
    }
}
