using Rojan.Desktop.Application.Accounting;

namespace Rojan.Desktop.Application.Tests.HR;

/// <summary>Minimal <see cref="IInvoiceQueryService"/> test double - only <see cref="GetInvoicesAsync"/> is exercised by <see cref="CommissionCommandServiceTests"/>.</summary>
internal sealed class StubInvoiceQueryService : IInvoiceQueryService
{
    private readonly IReadOnlyList<InvoiceDto> _invoices;

    public StubInvoiceQueryService(IReadOnlyList<InvoiceDto> invoices)
    {
        _invoices = invoices;
    }

    public Task<IReadOnlyList<InvoiceDto>> GetInvoicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_invoices);

    public Task<IReadOnlyList<InvoiceDto>> SearchInvoicesAsync(string searchText, CancellationToken cancellationToken = default) =>
        Task.FromResult(_invoices);

    public Task<InvoiceProfileDto> GetInvoiceProfileAsync(string invoiceId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<CheckoutOptionsDto> GetCheckoutOptionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new CheckoutOptionsDto([], [], [], []));
}
