using AppAccounting = Rojan.Desktop.Application.Accounting;
using AppBookings = Rojan.Desktop.Application.Bookings;
using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.HR;

/// <summary>
/// Default <see cref="ICommissionCommandService"/> implementation. Depends
/// on <see cref="AppAccounting.IInvoiceQueryService"/> and
/// <see cref="AppBookings.IBookingQueryService"/> to discover paid
/// invoices and the specialist who performed the booked service - the
/// same cross-slice composition reasoning as
/// <c>BookingWorkflow.BookingWorkflowService</c> and
/// <c>Accounting.InvoiceQueryService.GetCheckoutOptionsAsync</c>: normal
/// Clean Architecture use-case composition through published interfaces,
/// never a Domain dependency and never a modification to Accounting or
/// Bookings' own code.
/// </summary>
public sealed class CommissionCommandService : ICommissionCommandService
{
    private const decimal DefaultCommissionRate = 0.10m;

    private readonly DomainHr.IHrRepository _repository;
    private readonly AppAccounting.IInvoiceQueryService _invoiceQueryService;
    private readonly AppBookings.IBookingQueryService _bookingQueryService;

    public CommissionCommandService(
        DomainHr.IHrRepository repository,
        AppAccounting.IInvoiceQueryService invoiceQueryService,
        AppBookings.IBookingQueryService bookingQueryService)
    {
        _repository = repository;
        _invoiceQueryService = invoiceQueryService;
        _bookingQueryService = bookingQueryService;
    }

    public async Task<CommissionRuleDto> CreateCommissionRuleAsync(CreateCommissionRuleRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetEmployeeByIdAsync(request.EmployeeId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Employee '{request.EmployeeId}' was not found.");

        var rule = new DomainHr.CommissionRule(
            Guid.NewGuid().ToString(), employee.Id, employee.FullName,
            HrMapper.MapCommissionTypeToDomain(request.Type), request.Value, request.Description);

        var created = await _repository.CreateCommissionRuleAsync(rule, cancellationToken).ConfigureAwait(true);
        return HrMapper.MapCommissionRule(created);
    }

    public async Task<IReadOnlyList<CommissionTransactionDto>> GenerateCommissionsFromAccountingAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceQueryService.GetInvoicesAsync(cancellationToken).ConfigureAwait(true);
        var existingTransactions = await _repository.GetCommissionTransactionsAsync(cancellationToken).ConfigureAwait(true);
        var processedInvoiceIds = existingTransactions.Select(t => t.InvoiceId).ToHashSet(StringComparer.Ordinal);
        var employees = await _repository.GetEmployeesAsync(cancellationToken).ConfigureAwait(true);
        var rules = await _repository.GetCommissionRulesAsync(cancellationToken).ConfigureAwait(true);

        var generated = new List<CommissionTransactionDto>();

        var payableInvoices = invoices.Where(invoice =>
            invoice.Status is AppAccounting.InvoiceStatus.Paid or AppAccounting.InvoiceStatus.PartiallyPaid &&
            !processedInvoiceIds.Contains(invoice.Id) &&
            !string.IsNullOrEmpty(invoice.BookingId));

        foreach (var invoice in payableInvoices)
        {
            var booking = await _bookingQueryService.GetBookingByIdAsync(invoice.BookingId, cancellationToken).ConfigureAwait(true);
            if (booking is null || string.IsNullOrEmpty(booking.SpecialistId))
            {
                continue;
            }

            var employee = employees.FirstOrDefault(e => e.SpecialistId == booking.SpecialistId);
            if (employee is null)
            {
                continue;
            }

            var rule = rules.FirstOrDefault(r => r.EmployeeId == employee.Id)
                ?? new DomainHr.CommissionRule(string.Empty, employee.Id, employee.FullName, DomainHr.CommissionType.Percentage, DefaultCommissionRate, "Default rate");

            var commissionAmount = DomainHr.CommissionCalculator.ComputeCommission(rule, invoice.Total);

            var transaction = new DomainHr.CommissionTransaction(
                Guid.NewGuid().ToString(), employee.Id, employee.FullName, invoice.Id, booking.ServiceName,
                invoice.Total, commissionAmount, invoice.IssuedAt);

            var created = await _repository.CreateCommissionTransactionAsync(transaction, cancellationToken).ConfigureAwait(true);
            generated.Add(HrMapper.MapCommissionTransaction(created));
        }

        return generated;
    }
}
