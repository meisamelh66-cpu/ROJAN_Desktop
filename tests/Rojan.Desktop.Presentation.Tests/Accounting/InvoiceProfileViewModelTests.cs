using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Accounting;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Accounting;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Accounting;

public sealed class InvoiceProfileViewModelTests
{
    private const string FinancialSecret = "Amelia Hart / total 43.20 / Cash payment 43.20 / receipt";

    [Fact]
    public void LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoFinancialLeak()
    {
        var queryService = new StubInvoiceQueryService(
            _ => Task.FromResult<IReadOnlyList<InvoiceDto>>([]),
            getProfile: (_, _) => Task.FromException<InvoiceProfileDto>(new InvalidOperationException(FinancialSecret)));
        var logger = new RecordingLogger<InvoiceProfileViewModel>();

        var sut = new InvoiceProfileViewModel("invoice-1", queryService, logger);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.DoesNotContain(FinancialSecret, sut.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=LoadAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(FinancialSecret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows()
    {
        var queryService = new StubInvoiceQueryService(
            _ => Task.FromResult<IReadOnlyList<InvoiceDto>>([]),
            getProfile: (_, _) => Task.FromException<InvoiceProfileDto>(new InvalidOperationException("boom")));

        var sut = new InvoiceProfileViewModel("invoice-1", queryService);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
    }

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
    public void Constructor_ProfileQueryThrows_StateIsErrorAndSetsGenericErrorMessage()
    {
        var queryService = new StubInvoiceQueryService(
            _ => Task.FromResult<IReadOnlyList<InvoiceDto>>([]),
            getProfile: (_, _) => Task.FromException<InvoiceProfileDto>(new InvalidOperationException("boom")));

        var sut = new InvoiceProfileViewModel("invoice-1", queryService);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
    }
}
