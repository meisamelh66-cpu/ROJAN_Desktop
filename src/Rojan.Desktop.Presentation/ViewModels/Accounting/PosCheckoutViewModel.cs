using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Accounting;
using Rojan.Desktop.Presentation.Dialogs;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Accounting;

/// <summary>
/// Drives PosCheckoutView - a linear, step-by-step POS sale flow shown in
/// Shell's dialog region via <see cref="IDialogService"/>: Cart (pick a
/// customer, optionally an open booking, and add products/services) ->
/// Payment (charge a payment method against the invoice <see cref="InvoiceCommandService"/>
/// created from the cart) -> Receipt (confirmation). Fulfils both the "POS
/// checkout page" and "Payment dialog" deliverables in one dialog surface -
/// see <see cref="PosCheckoutStep"/>. Depends only on
/// <see cref="IInvoiceQueryService"/> (checkout options), <see cref="IInvoiceCommandService"/>
/// (invoice creation), <see cref="IPaymentCommandService"/> (charging), and
/// <see cref="IDialogService"/> - same small-dependency-surface shape as
/// <c>BookingWorkflow.BookingWizardViewModel</c>.
/// </summary>
public sealed class PosCheckoutViewModel : ViewModelBase
{
    /// <summary>
    /// Flat sales-tax rate this foundation POS applies to every sale -
    /// Phase 18 has no tax-configuration UI yet, same "no configuration
    /// surface built yet" scope-limit reasoning as every other module's
    /// first pass. Mirrors <c>Domain.Accounting.InvoiceCalculator.ComputeTax</c>'s
    /// rounding for the cart-step preview only; the authoritative total
    /// comes back from <see cref="IInvoiceCommandService.CreateInvoiceAsync"/>
    /// once the sale proceeds to Payment.
    /// </summary>
    private const decimal TaxRate = 0.08m;

    private readonly IInvoiceQueryService _invoiceQueryService;
    private readonly IInvoiceCommandService _invoiceCommandService;
    private readonly IPaymentCommandService _paymentCommandService;
    private readonly IDialogService _dialogService;
    private readonly Action? _onCompleted;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private PosCheckoutStep _currentStep = PosCheckoutStep.Cart;

    private CheckoutCustomerOptionDto? _selectedCustomer;
    private CheckoutBookingOptionDto? _selectedBooking;
    private CheckoutProductOptionDto? _selectedProductToAdd;
    private int _productQuantityToAdd = 1;
    private CheckoutServiceOptionDto? _selectedServiceToAdd;
    private int _serviceQuantityToAdd = 1;

    private InvoiceDto? _createdInvoice;
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;
    private decimal _amountTendered;
    private PaymentDto? _recordedPayment;

    public PosCheckoutViewModel(
        IInvoiceQueryService invoiceQueryService,
        IInvoiceCommandService invoiceCommandService,
        IPaymentCommandService paymentCommandService,
        IDialogService dialogService,
        Action? onCompleted = null)
    {
        _invoiceQueryService = invoiceQueryService;
        _invoiceCommandService = invoiceCommandService;
        _paymentCommandService = paymentCommandService;
        _dialogService = dialogService;
        _onCompleted = onCompleted;

        Customers = new ObservableCollection<CheckoutCustomerOptionDto>();
        Bookings = new ObservableCollection<CheckoutBookingOptionDto>();
        Products = new ObservableCollection<CheckoutProductOptionDto>();
        Services = new ObservableCollection<CheckoutServiceOptionDto>();
        Cart = new ObservableCollection<PosCartLine>();

        LoadCommand = new AsyncRelayCommand(_ => LoadOptionsAsync());
        AddProductCommand = new RelayCommand(_ => AddProduct(), _ => SelectedProductToAdd is not null && ProductQuantityToAdd > 0);
        AddServiceCommand = new RelayCommand(_ => AddService(), _ => SelectedServiceToAdd is not null && ServiceQuantityToAdd > 0);
        RemoveLineCommand = new RelayCommand(parameter => RemoveLine(parameter as PosCartLine));
        CancelCommand = new RelayCommand(_ => _dialogService.CloseDialog());
        ProceedToPaymentCommand = new AsyncRelayCommand(_ => ProceedToPaymentAsync(), _ => Cart.Count > 0 && SelectedCustomer is not null);
        ChargeCommand = new AsyncRelayCommand(_ => ChargeAsync(), _ => AmountTendered > 0 && CreatedInvoice is not null);
        DoneCommand = new RelayCommand(_ => _dialogService.CloseDialog());

        // Safe fire-and-forget: LoadOptionsAsync catches every failure
        // internally and represents it via State/ErrorMessage, same
        // reasoning as every other page ViewModel's constructor-time load.
        _ = LoadOptionsAsync();
    }

    public ObservableCollection<CheckoutCustomerOptionDto> Customers { get; }

    public ObservableCollection<CheckoutBookingOptionDto> Bookings { get; }

    public ObservableCollection<CheckoutProductOptionDto> Products { get; }

    public ObservableCollection<CheckoutServiceOptionDto> Services { get; }

    public ObservableCollection<PosCartLine> Cart { get; }

    public IReadOnlyList<PaymentMethod> AvailablePaymentMethods { get; } = Enum.GetValues<PaymentMethod>();

    public ICommand LoadCommand { get; }

    public ICommand AddProductCommand { get; }

    public ICommand AddServiceCommand { get; }

    public ICommand RemoveLineCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand ProceedToPaymentCommand { get; }

    public ICommand ChargeCommand { get; }

    public ICommand DoneCommand { get; }

    public DashboardState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public PosCheckoutStep CurrentStep
    {
        get => _currentStep;
        private set => SetProperty(ref _currentStep, value);
    }

    public CheckoutCustomerOptionDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    public CheckoutBookingOptionDto? SelectedBooking
    {
        get => _selectedBooking;
        set => SetProperty(ref _selectedBooking, value);
    }

    public CheckoutProductOptionDto? SelectedProductToAdd
    {
        get => _selectedProductToAdd;
        set => SetProperty(ref _selectedProductToAdd, value);
    }

    public int ProductQuantityToAdd
    {
        get => _productQuantityToAdd;
        set => SetProperty(ref _productQuantityToAdd, value);
    }

    public CheckoutServiceOptionDto? SelectedServiceToAdd
    {
        get => _selectedServiceToAdd;
        set => SetProperty(ref _selectedServiceToAdd, value);
    }

    public int ServiceQuantityToAdd
    {
        get => _serviceQuantityToAdd;
        set => SetProperty(ref _serviceQuantityToAdd, value);
    }

    public decimal Subtotal => Cart.Sum(line => line.LineTotal);

    public decimal TaxAmount => Math.Round(Subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);

    public decimal Total => Subtotal + TaxAmount;

    public InvoiceDto? CreatedInvoice
    {
        get => _createdInvoice;
        private set => SetProperty(ref _createdInvoice, value);
    }

    public PaymentMethod SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set => SetProperty(ref _selectedPaymentMethod, value);
    }

    public decimal AmountTendered
    {
        get => _amountTendered;
        set
        {
            if (SetProperty(ref _amountTendered, value))
            {
                OnPropertyChanged(nameof(ChangeDue));
            }
        }
    }

    /// <summary>Cash change owed - zero for every other payment method, and zero whenever the tendered amount does not exceed the invoice total (a partial/exact payment, not overpayment).</summary>
    public decimal ChangeDue => SelectedPaymentMethod == PaymentMethod.Cash && CreatedInvoice is not null && AmountTendered > CreatedInvoice.Total
        ? AmountTendered - CreatedInvoice.Total
        : 0m;

    public PaymentDto? RecordedPayment
    {
        get => _recordedPayment;
        private set => SetProperty(ref _recordedPayment, value);
    }

    private async Task LoadOptionsAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var options = await _invoiceQueryService.GetCheckoutOptionsAsync().ConfigureAwait(true);

            Customers.Clear();
            foreach (var customer in options.Customers)
            {
                Customers.Add(customer);
            }

            Bookings.Clear();
            foreach (var booking in options.Bookings)
            {
                Bookings.Add(booking);
            }

            Products.Clear();
            foreach (var product in options.Products)
            {
                Products.Add(product);
            }

            Services.Clear();
            foreach (var service in options.Services)
            {
                Services.Add(service);
            }

            State = Customers.Count == 0 ? DashboardState.Empty : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the dialog - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
        }
    }

    private void AddProduct()
    {
        if (SelectedProductToAdd is null || ProductQuantityToAdd <= 0)
        {
            return;
        }

        Cart.Add(new PosCartLine
        {
            ProductId = SelectedProductToAdd.Id,
            ServiceId = string.Empty,
            Description = SelectedProductToAdd.Name,
            Quantity = ProductQuantityToAdd,
            UnitPrice = SelectedProductToAdd.UnitPrice,
        });
        RaiseTotalsChanged();
        ProductQuantityToAdd = 1;
    }

    private void AddService()
    {
        if (SelectedServiceToAdd is null || ServiceQuantityToAdd <= 0)
        {
            return;
        }

        Cart.Add(new PosCartLine
        {
            ProductId = string.Empty,
            ServiceId = SelectedServiceToAdd.Id,
            Description = SelectedServiceToAdd.Name,
            Quantity = ServiceQuantityToAdd,
            UnitPrice = SelectedServiceToAdd.Price,
        });
        RaiseTotalsChanged();
        ServiceQuantityToAdd = 1;
    }

    private void RemoveLine(PosCartLine? line)
    {
        if (line is null)
        {
            return;
        }

        Cart.Remove(line);
        RaiseTotalsChanged();
    }

    private void RaiseTotalsChanged()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(TaxAmount));
        OnPropertyChanged(nameof(Total));
    }

    private async Task ProceedToPaymentAsync()
    {
        if (Cart.Count == 0 || SelectedCustomer is null)
        {
            return;
        }

        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var items = Cart
                .Select(line => new CreateInvoiceItemRequest(line.ProductId, line.ServiceId, line.Description, line.Quantity, line.UnitPrice))
                .ToList();

            var request = new CreateInvoiceRequest(
                SelectedCustomer.Id,
                SelectedCustomer.FullName,
                SelectedBooking?.Id ?? string.Empty,
                SelectedBooking?.Reference ?? string.Empty,
                items,
                TaxRate,
                string.Empty);

            CreatedInvoice = await _invoiceCommandService.CreateInvoiceAsync(request).ConfigureAwait(true);
            AmountTendered = CreatedInvoice.Total;
            CurrentStep = PosCheckoutStep.Payment;
            State = DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level command boundary: any failure must surface as the Error state, not crash the dialog - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
        }
    }

    private async Task ChargeAsync()
    {
        if (CreatedInvoice is null || AmountTendered <= 0)
        {
            return;
        }

        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var request = new RecordPaymentRequest(CreatedInvoice.Id, SelectedPaymentMethod, AmountTendered, string.Empty, string.Empty);
            RecordedPayment = await _paymentCommandService.RecordPaymentAsync(request).ConfigureAwait(true);
            CurrentStep = PosCheckoutStep.Receipt;
            State = DashboardState.Loaded;
            _onCompleted?.Invoke();
        }
#pragma warning disable CA1031 // Top-level command boundary: any failure must surface as the Error state, not crash the dialog - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
        }
    }
}
