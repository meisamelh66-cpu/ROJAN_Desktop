using DomainAccounting = Rojan.Desktop.Domain.Accounting;

namespace Rojan.Desktop.Application.Accounting;

/// <summary>Default <see cref="IPaymentQueryService"/> implementation.</summary>
public sealed class PaymentQueryService : IPaymentQueryService
{
    private readonly DomainAccounting.IAccountingRepository _repository;

    public PaymentQueryService(DomainAccounting.IAccountingRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PaymentDto>> GetPaymentsForInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        var payments = await _repository.GetPaymentsForInvoiceAsync(invoiceId, cancellationToken).ConfigureAwait(true);
        return payments.Select(AccountingMapper.MapPayment).ToList();
    }

    public async Task<RevenueSummaryDto> GetRevenueSummaryAsync(CancellationToken cancellationToken = default)
    {
        var payments = await _repository.GetPaymentsAsync(cancellationToken).ConfigureAwait(true);
        var invoices = await _repository.GetInvoicesAsync(cancellationToken).ConfigureAwait(true);

        var totalRevenue = payments.Sum(payment => payment.Amount);
        var today = DateTimeOffset.Now.Date;
        var todayRevenue = payments.Where(payment => payment.PaidAt.Date == today).Sum(payment => payment.Amount);

        var outstandingInvoices = invoices
            .Where(invoice => invoice.Status is DomainAccounting.InvoiceStatus.Issued or DomainAccounting.InvoiceStatus.PartiallyPaid)
            .ToList();
        var outstandingBalance = outstandingInvoices.Sum(invoice =>
            invoice.Total - payments.Where(payment => payment.InvoiceId == invoice.Id).Sum(payment => payment.Amount));

        var paidInvoiceCount = invoices.Count(invoice => invoice.Status == DomainAccounting.InvoiceStatus.Paid);

        return new RevenueSummaryDto(totalRevenue, todayRevenue, outstandingBalance, paidInvoiceCount, outstandingInvoices.Count);
    }

    public async Task<CashSessionDto?> GetOpenCashSessionAsync(CancellationToken cancellationToken = default)
    {
        var session = await _repository.GetOpenCashSessionAsync(cancellationToken).ConfigureAwait(true);
        return session is null ? null : AccountingMapper.MapCashSession(session);
    }
}
