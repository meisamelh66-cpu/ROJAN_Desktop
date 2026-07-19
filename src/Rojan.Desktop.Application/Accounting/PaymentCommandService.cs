using DomainAccounting = Rojan.Desktop.Domain.Accounting;

namespace Rojan.Desktop.Application.Accounting;

/// <summary>Default <see cref="IPaymentCommandService"/> implementation.</summary>
public sealed class PaymentCommandService : IPaymentCommandService
{
    private readonly DomainAccounting.IAccountingRepository _repository;

    public PaymentCommandService(DomainAccounting.IAccountingRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaymentDto> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (!DomainAccounting.InvoicePaymentRules.IsValidPaymentAmount(request.Amount))
        {
            throw new ArgumentException($"Payment amount {request.Amount} is not valid.", nameof(request));
        }

        var invoice = await _repository.GetInvoiceByIdAsync(request.InvoiceId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Invoice '{request.InvoiceId}' was not found.");

        if (invoice.Status == DomainAccounting.InvoiceStatus.Cancelled)
        {
            throw new InvalidOperationException($"Invoice '{request.InvoiceId}' is cancelled and cannot accept payment.");
        }

        var existingPayments = await _repository.GetPaymentsForInvoiceAsync(request.InvoiceId, cancellationToken).ConfigureAwait(true);
        var totalPaid = existingPayments.Sum(payment => payment.Amount) + request.Amount;
        var newStatus = DomainAccounting.InvoicePaymentRules.DetermineStatus(invoice.Total, totalPaid);

        var payment = new DomainAccounting.Payment(
            Guid.NewGuid().ToString(),
            request.InvoiceId,
            invoice.CustomerId,
            invoice.CustomerName,
            AccountingMapper.MapMethodToDomain(request.Method),
            request.Amount,
            DateTimeOffset.Now,
            request.CashSessionId,
            request.Notes);

        var recorded = await _repository.RecordPaymentAsync(payment, cancellationToken).ConfigureAwait(true);
        await _repository.UpdateInvoiceStatusAsync(request.InvoiceId, newStatus, cancellationToken).ConfigureAwait(true);

        var receipt = new DomainAccounting.Receipt(
            Guid.NewGuid().ToString(),
            recorded.Id,
            request.InvoiceId,
            DateTimeOffset.Now,
            request.Amount,
            invoice.CustomerName);
        await _repository.CreateReceiptAsync(receipt, cancellationToken).ConfigureAwait(true);

        return AccountingMapper.MapPayment(recorded);
    }

    public async Task<CashSessionDto> OpenCashSessionAsync(string cashierName, decimal openingFloat, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetOpenCashSessionAsync(cancellationToken).ConfigureAwait(true);
        if (existing is not null)
        {
            throw new InvalidOperationException("A cash session is already open.");
        }

        var session = new DomainAccounting.CashSession(
            Guid.NewGuid().ToString(),
            cashierName,
            DateTimeOffset.Now,
            null,
            openingFloat,
            null,
            DomainAccounting.CashSessionStatus.Open);

        var opened = await _repository.OpenCashSessionAsync(session, cancellationToken).ConfigureAwait(true);
        return AccountingMapper.MapCashSession(opened);
    }

    public async Task<CashSessionDto> CloseCashSessionAsync(string sessionId, decimal closingBalance, CancellationToken cancellationToken = default)
    {
        var closed = await _repository.CloseCashSessionAsync(sessionId, closingBalance, cancellationToken).ConfigureAwait(true);
        return AccountingMapper.MapCashSession(closed);
    }
}
