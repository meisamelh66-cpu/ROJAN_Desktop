using Rojan.Desktop.Application.Accounting;
using Rojan.Desktop.Presentation.ViewModels.Accounting;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Accounting;

public sealed class InvoiceProfileViewModelTests
{
    private static InvoiceDto MakeInvoice(string id) =>
        new(id, "customer-1", "Amelia Hart", string.Empty, string.Empty, DateTimeOffset.UnixEpoch, InvoiceStatus.Paid, 40m, 3.20m, 43.20m, string.Empty);

    private static InvoiceProfileDto MakeProfile(string id) => new(
        MakeInvoice(id),
        [new InvoiceItemDto("line-1", id, string.Empty, "service-4", "Manicure", 1, 40m, 40m)],
        [new PaymentDto("payment-1", id, "customer-1", "Amelia Hart", PaymentMethod.Cash, 43.20m, DateTimeOffset.UnixEpoch, string.Empty, string.Empty)],
        [new ReceiptDto("receipt-1", "payment-1", id, DateTimeOffset.UnixEpoch, 43.20m, "Amelia Hart")]);

    [Fact]
    public void Constructor_ProfileLoads_StateIsLoadedAndCollectionsPopulated()
    {
        var queryService = new StubInvoiceQueryService(
            _ => Task.FromResult<IReadOnlyList<InvoiceDto>>([]),
            getProfile: (invoiceId, _) => Task.FromResult(MakeProfile(invoiceId)));

        var sut = new InvoiceProfileViewModel("invoice-1", queryService);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal("invoice-1", sut.Invoice?.Id);
        Assert.Single(sut.Items);
        Assert.Single(sut.Payments);
        Assert.Single(sut.Receipts);
    }

    [Fact]
    public void Constructor_ProfileQueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubInvoiceQueryService(
            _ => Task.FromResult<IReadOnlyList<InvoiceDto>>([]),
            getProfile: (_, _) => Task.FromException<InvoiceProfileDto>(new InvalidOperationException("boom")));

        var sut = new InvoiceProfileViewModel("invoice-1", queryService);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }
}
