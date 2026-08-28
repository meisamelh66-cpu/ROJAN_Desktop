using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Accounting;
using Rojan.Desktop.Presentation.Dialogs;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Accounting;

/// <summary>
/// Drives AccountingPage - the Revenue KPI cards plus a searchable invoice
/// list on the left (with a "New Sale (POS)" button that opens
/// <see cref="PosCheckoutViewModel"/> in Shell's dialog region, same
/// <see cref="IDialogService"/> pattern <c>Bookings.BookingPageViewModel</c>
/// uses for its Booking Wizard), and the selected invoice's
/// <see cref="InvoiceProfileViewModel"/> on the right. Depends only on
/// Application services (<see cref="IInvoiceQueryService"/>,
/// <see cref="IInvoiceCommandService"/>, <see cref="IPaymentQueryService"/>,
/// <see cref="IPaymentCommandService"/>) plus <see cref="IDialogService"/>,
/// consistent with Presentation never reaching past Application into
/// Domain/Infrastructure.
/// </summary>
public sealed partial class AccountingPageViewModel : ViewModelBase
{
    private readonly IInvoiceQueryService _invoiceQueryService;
    private readonly IInvoiceCommandService _invoiceCommandService;
    private readonly IPaymentQueryService _paymentQueryService;
    private readonly IPaymentCommandService _paymentCommandService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<PosCheckoutViewModel>? _posCheckoutLogger;
    private readonly ILogger<AccountingPageViewModel> _logger;
    private readonly ILoggerFactory? _loggerFactory;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private string _searchText = string.Empty;
    private InvoiceDto? _selectedInvoice;
    private InvoiceProfileViewModel? _profile;
    private RevenueSummaryDto? _revenue;

    public AccountingPageViewModel(
        IInvoiceQueryService invoiceQueryService,
        IInvoiceCommandService invoiceCommandService,
        IPaymentQueryService paymentQueryService,
        IPaymentCommandService paymentCommandService,
        IDialogService dialogService,
        ILogger<PosCheckoutViewModel>? posCheckoutLogger = null,
        ILogger<AccountingPageViewModel>? logger = null,
        ILoggerFactory? loggerFactory = null)
    {
        _invoiceQueryService = invoiceQueryService;
        _invoiceCommandService = invoiceCommandService;
        _paymentQueryService = paymentQueryService;
        _paymentCommandService = paymentCommandService;
        _dialogService = dialogService;
        _posCheckoutLogger = posCheckoutLogger;
        _logger = logger ?? NullLogger<AccountingPageViewModel>.Instance;
        _loggerFactory = loggerFactory;

        Invoices = new ObservableCollection<InvoiceDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        OpenPosCheckoutCommand = new RelayCommand(_ => OpenPosCheckout());
        CancelInvoiceCommand = new AsyncRelayCommand(
            _ => CancelInvoiceAsync(),
            _ => SelectedInvoice is not null && SelectedInvoice.Status != InvoiceStatus.Cancelled);

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, same reasoning as every
        // other page ViewModel's constructor-time load.
        _ = LoadAsync();
    }

    public ObservableCollection<InvoiceDto> Invoices { get; }

    /// <summary>Re-runs the load - bound as the Retry action on DashboardWidget's Error state.</summary>
    public ICommand LoadCommand { get; }

    public ICommand OpenPosCheckoutCommand { get; }

    public ICommand CancelInvoiceCommand { get; }

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

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = SearchAsync(value);
            }
        }
    }

    public InvoiceDto? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (SetProperty(ref _selectedInvoice, value))
            {
                Profile = value is null ? null : new InvoiceProfileViewModel(value.Id, _invoiceQueryService, _loggerFactory?.CreateLogger<InvoiceProfileViewModel>());
            }
        }
    }

    /// <summary>Profile for <see cref="SelectedInvoice"/> - null when nothing is selected.</summary>
    public InvoiceProfileViewModel? Profile
    {
        get => _profile;
        private set => SetProperty(ref _profile, value);
    }

    /// <summary>The Revenue KPI card numbers - null until the first load completes.</summary>
    public RevenueSummaryDto? Revenue
    {
        get => _revenue;
        private set => SetProperty(ref _revenue, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var invoices = await _invoiceQueryService.GetInvoicesAsync().ConfigureAwait(true);
            ReplaceInvoices(invoices);

            Revenue = await _paymentQueryService.GetRevenueSummaryAsync().ConfigureAwait(true);

            State = invoices.Count == 0
                ? DashboardState.Empty
                : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
            LogOperationFailed(_logger, nameof(LoadAsync));
        }
    }

    /// <summary>
    /// Runs the search through <see cref="IInvoiceQueryService.SearchInvoicesAsync"/>
    /// rather than filtering a client-side cache - same reasoning as
    /// <c>Inventory.InventoryPageViewModel.SearchAsync</c>. Guards against
    /// out-of-order completions: if the user kept typing after this call
    /// started, <paramref name="searchText"/> no longer matches
    /// <see cref="SearchText"/> by the time the result arrives, and the
    /// stale result is discarded.
    /// </summary>
    private async Task SearchAsync(string searchText)
    {
        try
        {
            var results = await _invoiceQueryService.SearchInvoicesAsync(searchText).ConfigureAwait(true);
            if (!string.Equals(searchText, SearchText, StringComparison.Ordinal))
            {
                return;
            }

            ReplaceInvoices(results);
        }
#pragma warning disable CA1031 // Same top-level boundary reasoning as LoadAsync - a failed search must surface as the Error state, not crash the page.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            if (string.Equals(searchText, SearchText, StringComparison.Ordinal))
            {
                ErrorMessage = exception.Message;
                State = DashboardState.Error;
                LogOperationFailed(_logger, nameof(SearchAsync));
            }
        }
    }

    // Static form (ILogger passed explicitly) because this class holds two ILogger
    // fields - the source generator (SYSLIB1020) cannot pick one implicitly.
    // Operation name only: the caught exception is never passed to the logger
    // (Phase 8.15+ security rule - backend response bodies must not reach the log).
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Accounting operation failed. Operation={Operation}")]
    private static partial void LogOperationFailed(ILogger logger, string operation);

    private void ReplaceInvoices(IReadOnlyList<InvoiceDto> invoices)
    {
        Invoices.Clear();
        foreach (var invoice in invoices)
        {
            Invoices.Add(invoice);
        }

        if (SelectedInvoice is null || !Invoices.Contains(SelectedInvoice))
        {
            SelectedInvoice = Invoices.Count > 0 ? Invoices[0] : null;
        }
    }

    private async Task CancelInvoiceAsync()
    {
        if (SelectedInvoice is null)
        {
            return;
        }

        var invoiceId = SelectedInvoice.Id;
        await _invoiceCommandService.CancelInvoiceAsync(invoiceId).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
        SelectedInvoice = Invoices.FirstOrDefault(invoice => invoice.Id == invoiceId);
    }

    private void OpenPosCheckout()
    {
        var checkout = new PosCheckoutViewModel(_invoiceQueryService, _invoiceCommandService, _paymentCommandService, _dialogService, () => _ = LoadAsync(), _posCheckoutLogger);
        _dialogService.ShowDialog(checkout);
    }
}
