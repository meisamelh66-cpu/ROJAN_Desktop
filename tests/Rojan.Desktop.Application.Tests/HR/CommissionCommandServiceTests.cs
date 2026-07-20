using Rojan.Desktop.Application.HR;
using AppAccounting = Rojan.Desktop.Application.Accounting;
using AppBookings = Rojan.Desktop.Application.Bookings;
using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.Tests.HR;

public sealed class CommissionCommandServiceTests
{
    private static AppAccounting.InvoiceDto MakeInvoice(string id, string bookingId, AppAccounting.InvoiceStatus status, decimal total) =>
        new(id, "customer-1", "Amelia Hart", bookingId, string.Empty, DateTimeOffset.Now, status, total, 0m, total, string.Empty);

    private static AppBookings.BookingDto MakeBooking(string id, string specialistId, string serviceName) =>
        new(id, "customer-1", "Amelia Hart", "service-1", serviceName, specialistId, "Jordan Lee", DateTimeOffset.Now, 60, "$65", AppBookings.BookingStatus.Completed, string.Empty, "org-1", "branch-1");

    private static DomainHr.Employee MakeEmployee(string id, string specialistId, string fullName) =>
        new(id, specialistId, fullName, $"{id}@rojan.example", "+1 555", DomainHr.EmployeeRole.Colorist, DomainHr.Department.Hair, DomainHr.EmploymentType.FullTime, DomainHr.EmployeeStatus.Active, new DateOnly(2022, 1, 1), 3000m);

    private static CommissionCommandService MakeSut(
        StubHrRepository repository,
        AppAccounting.InvoiceDto[]? invoices = null,
        AppBookings.BookingDto[]? bookings = null) => new(
        repository,
        new StubInvoiceQueryService(invoices ?? []),
        new StubBookingQueryService(bookings ?? []));

    [Fact]
    public async Task GenerateCommissionsFromAccountingAsync_PaidInvoiceWithBookingAndMatchingEmployee_GeneratesCommission()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "specialist-1", "Jordan Lee"));
        repository.CommissionRules.Add(new DomainHr.CommissionRule("rule-1", "employee-1", "Jordan Lee", DomainHr.CommissionType.Percentage, 0.15m, string.Empty));
        var sut = MakeSut(
            repository,
            invoices: [MakeInvoice("invoice-1", "booking-1", AppAccounting.InvoiceStatus.Paid, 100m)],
            bookings: [MakeBooking("booking-1", "specialist-1", "Haircut & Style")]);

        var generated = await sut.GenerateCommissionsFromAccountingAsync();

        var transaction = Assert.Single(generated);
        Assert.Equal("employee-1", transaction.EmployeeId);
        Assert.Equal(15m, transaction.CommissionAmount);
        Assert.Single(repository.CommissionTransactions);
    }

    [Fact]
    public async Task GenerateCommissionsFromAccountingAsync_NoCommissionRule_UsesDefaultTenPercentRate()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "specialist-1", "Jordan Lee"));
        var sut = MakeSut(
            repository,
            invoices: [MakeInvoice("invoice-1", "booking-1", AppAccounting.InvoiceStatus.Paid, 100m)],
            bookings: [MakeBooking("booking-1", "specialist-1", "Haircut & Style")]);

        var generated = await sut.GenerateCommissionsFromAccountingAsync();

        var transaction = Assert.Single(generated);
        Assert.Equal(10m, transaction.CommissionAmount);
    }

    [Fact]
    public async Task GenerateCommissionsFromAccountingAsync_AlreadyProcessedInvoice_DoesNotGenerateDuplicate()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "specialist-1", "Jordan Lee"));
        repository.CommissionTransactions.Add(new DomainHr.CommissionTransaction("commission-existing", "employee-1", "Jordan Lee", "invoice-1", "Haircut & Style", 100m, 10m, DateTimeOffset.Now));
        var sut = MakeSut(
            repository,
            invoices: [MakeInvoice("invoice-1", "booking-1", AppAccounting.InvoiceStatus.Paid, 100m)],
            bookings: [MakeBooking("booking-1", "specialist-1", "Haircut & Style")]);

        var generated = await sut.GenerateCommissionsFromAccountingAsync();

        Assert.Empty(generated);
        Assert.Single(repository.CommissionTransactions);
    }

    [Fact]
    public async Task GenerateCommissionsFromAccountingAsync_InvoiceWithNoBooking_IsSkipped()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "specialist-1", "Jordan Lee"));
        var sut = MakeSut(
            repository,
            invoices: [MakeInvoice("invoice-1", string.Empty, AppAccounting.InvoiceStatus.Paid, 100m)]);

        var generated = await sut.GenerateCommissionsFromAccountingAsync();

        Assert.Empty(generated);
    }

    [Fact]
    public async Task GenerateCommissionsFromAccountingAsync_IssuedInvoiceNotYetPaid_IsSkipped()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "specialist-1", "Jordan Lee"));
        var sut = MakeSut(
            repository,
            invoices: [MakeInvoice("invoice-1", "booking-1", AppAccounting.InvoiceStatus.Issued, 100m)],
            bookings: [MakeBooking("booking-1", "specialist-1", "Haircut & Style")]);

        var generated = await sut.GenerateCommissionsFromAccountingAsync();

        Assert.Empty(generated);
    }

    [Fact]
    public async Task GenerateCommissionsFromAccountingAsync_NoEmployeeMatchesSpecialist_IsSkipped()
    {
        var repository = new StubHrRepository();
        var sut = MakeSut(
            repository,
            invoices: [MakeInvoice("invoice-1", "booking-1", AppAccounting.InvoiceStatus.Paid, 100m)],
            bookings: [MakeBooking("booking-1", "specialist-1", "Haircut & Style")]);

        var generated = await sut.GenerateCommissionsFromAccountingAsync();

        Assert.Empty(generated);
    }

    [Fact]
    public async Task GenerateCommissionsFromAccountingAsync_PartiallyPaidInvoice_StillGeneratesCommission()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "specialist-1", "Jordan Lee"));
        var sut = MakeSut(
            repository,
            invoices: [MakeInvoice("invoice-1", "booking-1", AppAccounting.InvoiceStatus.PartiallyPaid, 200m)],
            bookings: [MakeBooking("booking-1", "specialist-1", "Colour Touch-Up")]);

        var generated = await sut.GenerateCommissionsFromAccountingAsync();

        Assert.Single(generated);
    }

    [Fact]
    public async Task CreateCommissionRuleAsync_UnknownEmployee_ThrowsInvalidOperationException()
    {
        var repository = new StubHrRepository();
        var sut = MakeSut(repository);
        var request = new CreateCommissionRuleRequest("no-such-employee", CommissionType.Percentage, 0.10m, string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateCommissionRuleAsync(request));
    }

    [Fact]
    public async Task CreateCommissionRuleAsync_KnownEmployee_CreatesRule()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "specialist-1", "Jordan Lee"));
        var sut = MakeSut(repository);
        var request = new CreateCommissionRuleRequest("employee-1", CommissionType.Percentage, 0.12m, "Standard rate");

        var created = await sut.CreateCommissionRuleAsync(request);

        Assert.Equal("Jordan Lee", created.EmployeeName);
        Assert.Single(repository.CommissionRules);
    }
}
