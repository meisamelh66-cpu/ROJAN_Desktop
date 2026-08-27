using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Accounting;
using Rojan.Desktop.Presentation.Tests.Dialogs;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Accounting;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Accounting;

public sealed class PosCheckoutViewModelTests
{
    private static CheckoutCustomerOptionDto MakeCustomer() => new("customer-1", "Amelia Hart");

    private static CheckoutProductOptionDto MakeProduct() => new("product-1", "Hydrating Shampoo 1L", 18m);

    private static CheckoutServiceOptionDto MakeService() => new("service-4", "Manicure", 40m);

    private static CheckoutOptionsDto MakeOptions() =>
        new([MakeCustomer()], [], [MakeProduct()], [MakeService()]);

    private static InvoiceDto MakeCreatedInvoice(CreateInvoiceRequest request) => new(
        "invoice-new", request.CustomerId, request.CustomerName, request.BookingId, request.BookingReference,
        DateTimeOffset.Now, InvoiceStatus.Issued, 40m, 3.20m, 43.20m, request.Notes);

    private static PosCheckoutViewModel MakeSut(
        StubInvoiceQueryService? queryService = null,
        StubInvoiceCommandService? commandService = null,
        StubPaymentCommandService? paymentCommandService = null,
        StubDialogService? dialogService = null,
        Action? onCompleted = null,
        ILogger<PosCheckoutViewModel>? logger = null) => new(
        queryService ?? new StubInvoiceQueryService(_ => Task.FromResult<IReadOnlyList<InvoiceDto>>([]), getCheckoutOptions: _ => Task.FromResult(MakeOptions())),
        commandService ?? new StubInvoiceCommandService((request, _) => Task.FromResult(MakeCreatedInvoice(request))),
        paymentCommandService ?? new StubPaymentCommandService(),
        dialogService ?? new StubDialogService(),
        onCompleted,
        logger);

    [Fact]
    public void Constructor_OptionsLoad_StateIsLoadedAndCollectionsPopulated()
    {
        var sut = MakeSut();

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Single(sut.Customers);
        Assert.Single(sut.Products);
        Assert.Single(sut.Services);
        Assert.Equal(PosCheckoutStep.Cart, sut.CurrentStep);
    }

    [Fact]
    public void Constructor_OptionsQueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubInvoiceQueryService(
            _ => Task.FromResult<IReadOnlyList<InvoiceDto>>([]),
            getCheckoutOptions: _ => Task.FromException<CheckoutOptionsDto>(new InvalidOperationException("boom")));

        var sut = MakeSut(queryService);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void AddProductCommand_Executed_AddsLineToCartAndUpdatesTotals()
    {
        var sut = MakeSut();
        sut.SelectedProductToAdd = MakeProduct();
        sut.ProductQuantityToAdd = 2;

        sut.AddProductCommand.Execute(null);

        var line = Assert.Single(sut.Cart);
        Assert.Equal("product-1", line.ProductId);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(36m, sut.Subtotal);
    }

    [Fact]
    public void AddServiceCommand_Executed_AddsLineToCartAndUpdatesTotals()
    {
        var sut = MakeSut();
        sut.SelectedServiceToAdd = MakeService();
        sut.ServiceQuantityToAdd = 1;

        sut.AddServiceCommand.Execute(null);

        var line = Assert.Single(sut.Cart);
        Assert.Equal("service-4", line.ServiceId);
        Assert.Equal(40m, sut.Subtotal);
    }

    [Fact]
    public void RemoveLineCommand_Executed_RemovesLineAndUpdatesTotals()
    {
        var sut = MakeSut();
        sut.SelectedProductToAdd = MakeProduct();
        sut.AddProductCommand.Execute(null);
        var line = sut.Cart.Single();

        sut.RemoveLineCommand.Execute(line);

        Assert.Empty(sut.Cart);
        Assert.Equal(0m, sut.Subtotal);
    }

    [Fact]
    public void ProceedToPaymentCommand_CanExecute_FalseUntilCustomerSelectedAndCartHasItems()
    {
        var sut = MakeSut();

        Assert.False(sut.ProceedToPaymentCommand.CanExecute(null));

        sut.SelectedCustomer = MakeCustomer();
        Assert.False(sut.ProceedToPaymentCommand.CanExecute(null));

        sut.SelectedProductToAdd = MakeProduct();
        sut.AddProductCommand.Execute(null);
        Assert.True(sut.ProceedToPaymentCommand.CanExecute(null));
    }

    private static PosCheckoutViewModel MakeSutOnPaymentStep(
        StubInvoiceCommandService? commandService = null,
        StubPaymentCommandService? paymentCommandService = null,
        StubDialogService? dialogService = null,
        Action? onCompleted = null,
        ILogger<PosCheckoutViewModel>? logger = null)
    {
        var sut = MakeSut(commandService: commandService, paymentCommandService: paymentCommandService, dialogService: dialogService, onCompleted: onCompleted, logger: logger);
        sut.SelectedCustomer = MakeCustomer();
        sut.SelectedProductToAdd = MakeProduct();
        sut.AddProductCommand.Execute(null);
        sut.ProceedToPaymentCommand.Execute(null);
        return sut;
    }

    [Fact]
    public void ProceedToPaymentCommand_Executed_CreatesInvoiceAndAdvancesToPaymentStep()
    {
        var commandService = new StubInvoiceCommandService((request, _) => Task.FromResult(MakeCreatedInvoice(request)));
        var sut = MakeSutOnPaymentStep(commandService);

        Assert.Equal(PosCheckoutStep.Payment, sut.CurrentStep);
        Assert.NotNull(sut.CreatedInvoice);
        Assert.Equal(43.20m, sut.AmountTendered);
        var request = Assert.Single(commandService.CreateRequests);
        Assert.Equal("customer-1", request.CustomerId);
        Assert.Single(request.Items);
    }

    [Fact]
    public void ChargeCommand_CanExecute_FalseWhenAmountTenderedIsZero()
    {
        var sut = MakeSutOnPaymentStep();

        sut.AmountTendered = 0m;

        Assert.False(sut.ChargeCommand.CanExecute(null));
    }

    [Fact]
    public void ChargeCommand_Executed_RecordsPaymentAdvancesToReceiptAndInvokesOnCompleted()
    {
        var completed = false;
        var paymentCommandService = new StubPaymentCommandService();
        var sut = MakeSutOnPaymentStep(paymentCommandService: paymentCommandService, onCompleted: () => completed = true);

        sut.ChargeCommand.Execute(null);

        Assert.Equal(PosCheckoutStep.Receipt, sut.CurrentStep);
        Assert.NotNull(sut.RecordedPayment);
        Assert.True(completed);
        var request = Assert.Single(paymentCommandService.RecordRequests);
        Assert.Equal("invoice-new", request.InvoiceId);
        Assert.Equal(43.20m, request.Amount);
    }

    [Fact]
    public void ChangeDue_CashOverpayment_ReturnsDifference()
    {
        var sut = MakeSutOnPaymentStep();
        sut.SelectedPaymentMethod = PaymentMethod.Cash;

        sut.AmountTendered = 50m;

        Assert.Equal(6.80m, sut.ChangeDue);
    }

    [Fact]
    public void ChangeDue_NonCashMethod_ReturnsZeroEvenWhenOverpaid()
    {
        var sut = MakeSutOnPaymentStep();
        sut.SelectedPaymentMethod = PaymentMethod.Card;

        sut.AmountTendered = 50m;

        Assert.Equal(0m, sut.ChangeDue);
    }

    [Fact]
    public void CancelCommand_Executed_ClosesDialog()
    {
        var dialogService = new StubDialogService();
        var sut = MakeSut(dialogService: dialogService);

        sut.CancelCommand.Execute(null);

        Assert.Equal(1, dialogService.CloseCount);
    }

    [Fact]
    public void DoneCommand_Executed_ClosesDialog()
    {
        var dialogService = new StubDialogService();
        var sut = MakeSut(dialogService: dialogService);

        sut.DoneCommand.Execute(null);

        Assert.Equal(1, dialogService.CloseCount);
    }

    // Phase 7.4.4 Booking/Checkout Error Hardening: this ViewModel already had a real try/catch on
    // every backend-touching method - these tests verify the newly-added logging actually fires,
    // not the error-state behavior itself (already covered above/pre-existing).

    [Fact]
    public void LoadCommand_QueryThrows_LogsTheFailure()
    {
        var logger = new RecordingLogger<PosCheckoutViewModel>();
        var queryService = new StubInvoiceQueryService(_ => Task.FromResult<IReadOnlyList<InvoiceDto>>([]), getCheckoutOptions: _ => Task.FromException<CheckoutOptionsDto>(new InvalidOperationException("boom")));

        var sut = MakeSut(queryService, logger: logger);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public void ProceedToPaymentCommand_BackendThrows_LogsTheFailure()
    {
        var logger = new RecordingLogger<PosCheckoutViewModel>();
        var commandService = new StubInvoiceCommandService((_, _) => Task.FromException<InvoiceDto>(new InvalidOperationException("boom")));
        var sut = MakeSut(commandService: commandService, logger: logger);
        sut.SelectedCustomer = MakeCustomer();
        sut.SelectedProductToAdd = MakeProduct();
        sut.AddProductCommand.Execute(null);

        sut.ProceedToPaymentCommand.Execute(null);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public void ChargeCommand_BackendThrows_LogsTheFailureAndLeavesInvoiceReChargeable()
    {
        var logger = new RecordingLogger<PosCheckoutViewModel>();
        var paymentCommandService = new StubPaymentCommandService((_, _) => Task.FromException<PaymentDto>(new InvalidOperationException("boom")));
        var sut = MakeSutOnPaymentStep(paymentCommandService: paymentCommandService, logger: logger);
        var invoiceBeforeCharge = sut.CreatedInvoice;

        sut.ChargeCommand.Execute(null);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
        // Flagged in the audit report, not fixed here (payment business logic is out of scope):
        // CreatedInvoice/AmountTendered are left unchanged after a failed charge, so ChargeCommand
        // can be re-invoked against the same invoice - this test documents that current behavior
        // rather than silently assuming it.
        Assert.Same(invoiceBeforeCharge, sut.CreatedInvoice);
        Assert.True(sut.ChargeCommand.CanExecute(null));
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_ChargeFailureNeverThrows()
    {
        var paymentCommandService = new StubPaymentCommandService((_, _) => Task.FromException<PaymentDto>(new InvalidOperationException("boom")));
        var sut = MakeSutOnPaymentStep(paymentCommandService: paymentCommandService);

        var exception = Record.Exception(() => sut.ChargeCommand.Execute(null));

        Assert.Null(exception);
        Assert.Equal(DashboardState.Error, sut.State);
    }
}
