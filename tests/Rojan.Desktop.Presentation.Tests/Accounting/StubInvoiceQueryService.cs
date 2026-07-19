using Rojan.Desktop.Application.Accounting;

namespace Rojan.Desktop.Presentation.Tests.Accounting;

/// <summary>Configurable <see cref="IInvoiceQueryService"/> test double - same reasoning as Inventory.StubProductQueryService.</summary>
internal sealed class StubInvoiceQueryService : IInvoiceQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<InvoiceDto>>> _getInvoices;
    private readonly Func<string, CancellationToken, Task<IReadOnlyList<InvoiceDto>>>? _searchInvoices;
    private readonly Func<string, CancellationToken, Task<InvoiceProfileDto>>? _getProfile;
    private readonly Func<CancellationToken, Task<CheckoutOptionsDto>>? _getCheckoutOptions;

    public StubInvoiceQueryService(
        Func<CancellationToken, Task<IReadOnlyList<InvoiceDto>>> getInvoices,
        Func<string, CancellationToken, Task<IReadOnlyList<InvoiceDto>>>? searchInvoices = null,
        Func<string, CancellationToken, Task<InvoiceProfileDto>>? getProfile = null,
        Func<CancellationToken, Task<CheckoutOptionsDto>>? getCheckoutOptions = null)
    {
        _getInvoices = getInvoices;
        _searchInvoices = searchInvoices;
        _getProfile = getProfile;
        _getCheckoutOptions = getCheckoutOptions;
    }

    public Task<IReadOnlyList<InvoiceDto>> GetInvoicesAsync(CancellationToken cancellationToken = default) =>
        _getInvoices(cancellationToken);

    public async Task<IReadOnlyList<InvoiceDto>> SearchInvoicesAsync(string searchText, CancellationToken cancellationToken = default)
    {
        if (_searchInvoices is not null)
        {
            return await _searchInvoices(searchText, cancellationToken).ConfigureAwait(true);
        }

        var invoices = await _getInvoices(cancellationToken).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return invoices;
        }

        return invoices
            .Where(invoice => invoice.CustomerName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public Task<InvoiceProfileDto> GetInvoiceProfileAsync(string invoiceId, CancellationToken cancellationToken = default) =>
        _getProfile?.Invoke(invoiceId, cancellationToken) ?? throw new NotSupportedException();

    public Task<CheckoutOptionsDto> GetCheckoutOptionsAsync(CancellationToken cancellationToken = default) =>
        _getCheckoutOptions?.Invoke(cancellationToken) ?? Task.FromResult(new CheckoutOptionsDto([], [], [], []));
}
