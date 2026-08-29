using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Accounting;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Accounting;

/// <summary>
/// Drives the read-only invoice detail panel - line items, payments, and
/// receipts for one selected invoice. Owned by
/// <see cref="AccountingPageViewModel"/>, constructed fresh whenever the
/// selected invoice changes - same per-selection child-ViewModel pattern
/// <c>Inventory.InventoryProfileViewModel</c> established in Phase 17.
/// Deliberately read-only: invoice mutation (cancel) lives on the parent
/// page ViewModel, same as <c>Bookings.BookingPageViewModel.ChangeStatusAsync</c>
/// operating on its own SelectedBooking rather than a child profile.
/// </summary>
public sealed partial class InvoiceProfileViewModel : ViewModelBase
{
    private readonly string _invoiceId;
    private readonly IInvoiceQueryService _queryService;
    private readonly ILogger<InvoiceProfileViewModel> _logger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private InvoiceDto? _invoice;

    public InvoiceProfileViewModel(
        string invoiceId,
        IInvoiceQueryService queryService,
        ILogger<InvoiceProfileViewModel>? logger = null)
    {
        _invoiceId = invoiceId;
        _queryService = queryService;
        _logger = logger ?? NullLogger<InvoiceProfileViewModel>.Instance;

        Items = new ObservableCollection<InvoiceItemDto>();
        Payments = new ObservableCollection<PaymentDto>();
        Receipts = new ObservableCollection<ReceiptDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, same pattern as every
        // other page/profile ViewModel in this app.
        _ = LoadAsync();
    }

    public ObservableCollection<InvoiceItemDto> Items { get; }

    public ObservableCollection<PaymentDto> Payments { get; }

    public ObservableCollection<ReceiptDto> Receipts { get; }

    public ICommand LoadCommand { get; }

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

    public InvoiceDto? Invoice
    {
        get => _invoice;
        private set => SetProperty(ref _invoice, value);
    }

    public async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var profile = await _queryService.GetInvoiceProfileAsync(_invoiceId).ConfigureAwait(true);

            Invoice = profile.Invoice;

            Items.Clear();
            foreach (var item in profile.Items)
            {
                Items.Add(item);
            }

            Payments.Clear();
            foreach (var payment in profile.Payments)
            {
                Payments.Add(payment);
            }

            Receipts.Clear();
            foreach (var receipt in profile.Receipts)
            {
                Receipts.Add(receipt);
            }

            State = DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page/profile ViewModel in this app.
        catch (Exception)
#pragma warning restore CA1031
        {
            ErrorMessage = Strings.Common_ActionFailedMessage;
            State = DashboardState.Error;
            LogOperationFailed(nameof(LoadAsync));
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Invoice profile operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);
}
