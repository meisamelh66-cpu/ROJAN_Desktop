namespace Rojan.Desktop.Application.Accounting;

/// <summary>Read-only use cases Presentation depends on to load Payments and the Revenue KPI/cash-session data derived from them.</summary>
public interface IPaymentQueryService
{
    public Task<IReadOnlyList<PaymentDto>> GetPaymentsForInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default);

    /// <summary>The Revenue KPI card numbers - composes over payments/invoices, same "read the set, compose in Application" convention every other module's derived numbers follow.</summary>
    public Task<RevenueSummaryDto> GetRevenueSummaryAsync(CancellationToken cancellationToken = default);

    public Task<CashSessionDto?> GetOpenCashSessionAsync(CancellationToken cancellationToken = default);
}
