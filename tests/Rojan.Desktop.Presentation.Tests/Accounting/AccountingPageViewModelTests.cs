using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Accounting;
using Rojan.Desktop.Presentation.Tests.Dialogs;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Accounting;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Accounting;

public sealed class AccountingPageViewModelTests
{
    private static InvoiceDto MakeInvoice(string id, string customerName, InvoiceStatus status = InvoiceStatus.Issued) =>
        new(id, "customer-1", customerName, string.Empty, string.Empty, DateTimeOffset.UnixEpoch, status, 40m, 3.20m, 43.20m, string.Empty);

    private static InvoiceProfileDto MakeProfile(string id) => new(
        MakeInvoice(id, "Amelia Hart"), [], [], []);

    private static AccountingPageViewModel MakeSut(
        StubInvoiceQueryService queryService,
        StubInvoiceCommandService? commandService = null,
        StubPaymentQueryService? paymentQueryService = null,
        StubPaymentCommandService? paymentCommandService = null,
        StubDialogService? dialogService = null,
        RecordingLogger<AccountingPageViewModel>? logger = null,
        RecordingLoggerFactory? loggerFactory = null) => new(
        queryService,
        commandService ?? new StubInvoiceCommandService(),
        paymentQueryService ?? new StubPaymentQueryService(),
        paymentCommandService ?? new StubPaymentCommandService(),
        dialogService ?? new StubDialogService(),
        logger: logger,
        loggerFactory: loggerFactory);

    [Fact]
    public void LoggerFactory_ForwardedToInvoiceProfileChild_ChildLoadFailureIsLoggedViaTheFactory()
    {
        const string secret = "child invoice financial secret / total 43.20 / Cash payment";
        var invoices = new List<InvoiceDto> { MakeInvoice("invoice-1", "Amelia Hart") };
        var queryService = new StubInvoiceQueryService(
            _ => Task.FromResult<IReadOnlyList<InvoiceDto>>(invoices),
            getProfile: (_, _) => Task.FromException<InvoiceProfileDto>(new InvalidOperationException(secret)));
        var loggerFactory = new RecordingLoggerFactory();

        var sut = MakeSut(queryService, loggerFactory: loggerFactory);

        Assert.NotNull(sut.Profile);
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains(nameof(InvoiceProfileViewModel), entry.Category, StringComparison.Ordinal);
        Assert.Contains("Operation=LoadAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_QueryServiceStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<InvoiceDto>>();
        var queryService = new StubInvoiceQueryService(_ => tcs.Task);

        var sut = MakeSut(queryService);

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsInvoices_StateIsLoadedAndPopulatesInvoicesAndSelectsFirst()
    {
        var invoices = new List<InvoiceDto> { MakeInvoice("invoice-1", "Amelia Hart") };
        var queryService = new StubInvoiceQueryService(
            _ => Task.FromResult<IReadOnlyList<InvoiceDto>>(invoices),
            getProfile: (invoiceId, _) => Task.FromResult(MakeProfile(invoiceId)));

        var sut = MakeSut(queryService);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(invoices, sut.Invoices);
        Assert.Equal(invoices[0], sut.SelectedInvoice);
        Assert.NotNull(sut.Profile);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsEmptyList_StateIsEmpty()
    {
        var queryService = new StubInvoiceQueryService(_ => Task.FromResult<IReadOnlyList<InvoiceDto>>([]));

        var sut = MakeSut(queryService);

        Assert.Equal(DashboardState.Empty, sut.State);
        Assert.Null(sut.SelectedInvoice);
        Assert.Null(sut.Profile);
    }

    [Fact]
    public void Constructor_QueryServiceThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubInvoiceQueryService(
            _ => Task.FromException<IReadOnlyList<InvoiceDto>>(new InvalidOperationException("boom")));

        var sut = MakeSut(queryService);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    // Phase 8.11 Logging Hardening: LoadAsync and SearchAsync now log at Error
    // before surfacing the Error state - user-visible behaviour unchanged.

    [Fact]
    public void LoadAsync_QueryServiceThrows_LogsErrorWithOperation()
    {
        var queryService = new StubInvoiceQueryService(
            _ => Task.FromException<IReadOnlyList<InvoiceDto>>(new InvalidOperationException("boom")));
        var logger = new RecordingLogger<AccountingPageViewModel>();

        var sut = MakeSut(queryService, logger: logger);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("LoadAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void SearchAsync_QueryServiceThrows_LogsErrorWithOperation()
    {
        var invoices = new List<InvoiceDto> { MakeInvoice("invoice-1", "Amelia Hart") };
        var queryService = new StubInvoiceQueryService(
            _ => Task.FromResult<IReadOnlyList<InvoiceDto>>(invoices),
            searchInvoices: (_, _) => Task.FromException<IReadOnlyList<InvoiceDto>>(new InvalidOperationException("search boom")),
            getProfile: (invoiceId, _) => Task.FromResult(MakeProfile(invoiceId)));
        var logger = new RecordingLogger<AccountingPageViewModel>();
        var sut = MakeSut(queryService, logger: logger);

        sut.SearchText = "sophia";

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("SearchAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows()
    {
        var queryService = new StubInvoiceQueryService(
            _ => Task.FromException<IReadOnlyList<InvoiceDto>>(new InvalidOperationException("boom")));

        var exception = Record.Exception(() => MakeSut(queryService));

        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_LoadsRevenueSummaryFromPaymentQueryService()
    {
        var invoices = new List<InvoiceDto> { MakeInvoice("invoice-1", "Amelia Hart") };
        var queryService = new StubInvoiceQueryService(
            _ => Task.FromResult<IReadOnlyList<InvoiceDto>>(invoices),
            getProfile: (invoiceId, _) => Task.FromResult(MakeProfile(invoiceId)));
        var paymentQueryService = new StubPaymentQueryService(_ => Task.FromResult(new RevenueSummaryDto(500m, 100m, 50m, 3, 1)));

        var sut = MakeSut(queryService, paymentQueryService: paymentQueryService);

        Assert.NotNull(sut.Revenue);
        Assert.Equal(500m, sut.Revenue.TotalRevenue);
        Assert.Equal(3, sut.Revenue.PaidInvoiceCount);
    }

    [Fact]
    public void SearchText_MatchesCustomerName_FiltersToMatchingInvoicesOnly()
    {
        var invoices = new List<InvoiceDto>
        {
            MakeInvoice("invoice-1", "Amelia Hart"),
            MakeInvoice("invoice-2", "Sophia Reyes"),
        };
        var queryService = new StubInvoiceQueryService(
            _ => Task.FromResult<IReadOnlyList<InvoiceDto>>(invoices),
            getProfile: (invoiceId, _) => Task.FromResult(MakeProfile(invoiceId)));
        var sut = MakeSut(queryService);

        sut.SearchText = "sophia";

        Assert.Equal(["invoice-2"], sut.Invoices.Select(i => i.Id));
    }

    [Fact]
    public void CancelInvoiceCommand_CanExecute_FalseWhenNoSelectionOrAlreadyCancelled()
    {
        var queryService = new StubInvoiceQueryService(_ => Task.FromResult<IReadOnlyList<InvoiceDto>>([]));
        var sut = MakeSut(queryService);

        Assert.False(sut.CancelInvoiceCommand.CanExecute(null));
    }

    [Fact]
    public void CancelInvoiceCommand_Executed_CallsCommandServiceAndReloads()
    {
        var invoices = new List<InvoiceDto> { MakeInvoice("invoice-1", "Amelia Hart") };
        var queryService = new StubInvoiceQueryService(
            _ => Task.FromResult<IReadOnlyList<InvoiceDto>>(invoices),
            getProfile: (invoiceId, _) => Task.FromResult(MakeProfile(invoiceId)));
        var commandService = new StubInvoiceCommandService();
        var sut = MakeSut(queryService, commandService);

        sut.CancelInvoiceCommand.Execute(null);

        var call = Assert.Single(commandService.CancelledInvoiceIds);
        Assert.Equal("invoice-1", call);
    }

    [Fact]
    public void OpenPosCheckoutCommand_Executed_ShowsPosCheckoutViewModelInDialogService()
    {
        var queryService = new StubInvoiceQueryService(_ => Task.FromResult<IReadOnlyList<InvoiceDto>>([]));
        var dialogService = new StubDialogService();
        var sut = MakeSut(queryService, dialogService: dialogService);

        sut.OpenPosCheckoutCommand.Execute(null);

        var shown = Assert.Single(dialogService.ShownDialogs);
        Assert.IsType<PosCheckoutViewModel>(shown);
    }
}
