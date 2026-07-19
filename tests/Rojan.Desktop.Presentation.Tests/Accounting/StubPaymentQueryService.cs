using Rojan.Desktop.Application.Accounting;

namespace Rojan.Desktop.Presentation.Tests.Accounting;

/// <summary>Configurable <see cref="IPaymentQueryService"/> test double - same reasoning as Inventory.StubProductQueryService.</summary>
internal sealed class StubPaymentQueryService : IPaymentQueryService
{
    private readonly Func<CancellationToken, Task<RevenueSummaryDto>>? _getRevenueSummary;

    public StubPaymentQueryService(Func<CancellationToken, Task<RevenueSummaryDto>>? getRevenueSummary = null)
    {
        _getRevenueSummary = getRevenueSummary;
    }

    public Task<IReadOnlyList<PaymentDto>> GetPaymentsForInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PaymentDto>>([]);

    public Task<RevenueSummaryDto> GetRevenueSummaryAsync(CancellationToken cancellationToken = default) =>
        _getRevenueSummary?.Invoke(cancellationToken) ?? Task.FromResult(new RevenueSummaryDto(0m, 0m, 0m, 0, 0));

    public Task<CashSessionDto?> GetOpenCashSessionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<CashSessionDto?>(null);
}
