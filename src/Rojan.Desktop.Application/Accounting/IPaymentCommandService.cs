namespace Rojan.Desktop.Application.Accounting;

/// <summary>Write use cases for Payments and cash drawer sessions - the POS checkout's payment step, plus opening/closing the register.</summary>
public interface IPaymentCommandService
{
    /// <summary>Records a payment and re-derives the invoice's status via <c>Domain.Accounting.InvoicePaymentRules</c>, then issues a receipt - the two/three writes that together are "recording a payment".</summary>
    public Task<PaymentDto> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken cancellationToken = default);

    public Task<CashSessionDto> OpenCashSessionAsync(string cashierName, decimal openingFloat, CancellationToken cancellationToken = default);

    public Task<CashSessionDto> CloseCashSessionAsync(string sessionId, decimal closingBalance, CancellationToken cancellationToken = default);
}
