using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Membership;
using Rojan.Desktop.Application.QrCodes;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.QrCodes;

/// <summary>
/// QR Ecosystem (Desktop Productionization Sprint 1). Drives QrCodesPage -
/// three independent QR sources, each loaded/generated differently:
/// <see cref="ManagerQrBytes"/> is generated client-side on construction
/// (no backend call, always available - a static link to the Manager
/// app's download page); <see cref="CustomerQrBytes"/> is fetched from
/// ROJAN_Backend's real <c>/salons/{id}/qr-code</c> endpoint as part of
/// the same initial load as the salon's own branding info (name/phone/
/// address, needed for the print sheet); <see cref="ReceptionInviteQrBytes"/>
/// stays <see langword="null"/> until <see cref="GenerateReceptionInviteCommand"/>
/// is explicitly invoked - a fresh invite token is a real, single-purpose
/// backend resource, not something to mint silently every time this page
/// opens.
///
/// Deliberately holds raw <see cref="byte"/>[] rather than a WPF
/// <c>BitmapImage</c> - <see cref="Mvvm.ViewModelBase"/>'s own doc comment
/// is explicit that ViewModels stay free of WPF types so they're testable
/// without a UI thread; <c>Converters.ByteArrayToBitmapImageConverter</c>
/// does the byte[]-to-displayable-image conversion at the View's own
/// binding boundary instead, and <c>QrCodesPage.xaml.cs</c>'s print flow
/// reads these same raw bytes directly (a FixedDocument needs the PNG
/// bytes, not a BitmapImage, to lay out for printing).
/// </summary>
public sealed partial class QrCodesPageViewModel : ViewModelBase
{
    /// <summary>The Manager app's real (if not-yet-`available`) download page - see ROJAN_Web's own <c>app-showcase.ts</c>/the new <c>/download/[appId]</c> route this sprint adds.</summary>
    public const string ManagerDownloadUrl = "https://rojanai.ir/download/manager";

    private const int QrSizePx = 512;

    private readonly ISalonQueryService _salonQueryService;
    private readonly ISalonInviteService _salonInviteService;
    private readonly IStaticQrCodeGenerator _staticQrCodeGenerator;
    private readonly ILogger<QrCodesPageViewModel> _logger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private SalonDto? _salon;
    private byte[]? _managerQrBytes;
    private byte[]? _customerQrBytes;
    private byte[]? _receptionInviteQrBytes;
    private bool _isGeneratingReceptionInvite;
    private string? _generateInviteErrorMessage;

    public QrCodesPageViewModel(ISalonQueryService salonQueryService, ISalonInviteService salonInviteService, IStaticQrCodeGenerator staticQrCodeGenerator, ILogger<QrCodesPageViewModel>? logger = null)
    {
        _salonQueryService = salonQueryService;
        _salonInviteService = salonInviteService;
        _staticQrCodeGenerator = staticQrCodeGenerator;
        _logger = logger ?? NullLogger<QrCodesPageViewModel>.Instance;

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        GenerateReceptionInviteCommand = new AsyncRelayCommand(_ => GenerateReceptionInviteAsync(), _ => !IsGeneratingReceptionInvite);

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, same reasoning as every
        // other page ViewModel's constructor-time load (e.g. SalonPageViewModel).
        _ = LoadAsync();
    }

    public ICommand LoadCommand { get; }

    public ICommand GenerateReceptionInviteCommand { get; }

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

    /// <summary>The current salon's branding info (name/phone/address) for the print sheet - <see langword="null"/> until loaded, same as every other salon-scoped page.</summary>
    public SalonDto? Salon
    {
        get => _salon;
        private set => SetProperty(ref _salon, value);
    }

    public byte[]? ManagerQrBytes
    {
        get => _managerQrBytes;
        private set => SetProperty(ref _managerQrBytes, value);
    }

    public byte[]? CustomerQrBytes
    {
        get => _customerQrBytes;
        private set => SetProperty(ref _customerQrBytes, value);
    }

    public byte[]? ReceptionInviteQrBytes
    {
        get => _receptionInviteQrBytes;
        private set
        {
            if (SetProperty(ref _receptionInviteQrBytes, value))
            {
                OnPropertyChanged(nameof(HasReceptionInviteQr));
            }
        }
    }

    /// <summary>Drives QrCodesPage's "generate" prompt vs. the QR image itself, same "boolean flag picks between two panel states" shape <c>SalonPageViewModel.HasSalon</c> already establishes.</summary>
    public bool HasReceptionInviteQr => ReceptionInviteQrBytes is not null;

    public bool IsGeneratingReceptionInvite
    {
        get => _isGeneratingReceptionInvite;
        private set => SetProperty(ref _isGeneratingReceptionInvite, value);
    }

    public string? GenerateInviteErrorMessage
    {
        get => _generateInviteErrorMessage;
        private set
        {
            if (SetProperty(ref _generateInviteErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasGenerateInviteError));
            }
        }
    }

    public bool HasGenerateInviteError => !string.IsNullOrEmpty(GenerateInviteErrorMessage);

    /// <summary>Every QR needed for the print sheet is ready - the Print button's <c>CanExecute</c> in the View's code-behind checks this rather than duplicating the same three-source readiness logic there.</summary>
    public bool IsReadyToPrint => Salon is not null && ManagerQrBytes is not null && CustomerQrBytes is not null;

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            ManagerQrBytes = _staticQrCodeGenerator.GeneratePng(ManagerDownloadUrl, QrSizePx);

            Salon = await _salonQueryService.GetMySalonAsync().ConfigureAwait(true);
            CustomerQrBytes = await _salonQueryService.GetSalonQrCodeAsync(QrSizePx).ConfigureAwait(true);

            State = DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
            LogOperationFailed(nameof(LoadAsync));
        }
    }

    // Security: logs the operation name only - never the exception, its message,
    // salon/invite detail, or any backend response.
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "QR codes page operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);

    private async Task GenerateReceptionInviteAsync()
    {
        if (Salon is null)
        {
            return;
        }

        IsGeneratingReceptionInvite = true;
        GenerateInviteErrorMessage = null;

        try
        {
            var invite = await _salonInviteService.CreateReceptionInviteAsync(Salon.Id).ConfigureAwait(true);
            ReceptionInviteQrBytes = await _salonInviteService.GetInviteQrCodeAsync(Salon.Id, invite.InviteId, QrSizePx).ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Top-level command boundary: any failure must surface via GenerateInviteErrorMessage, not crash the page - same justified broad catch as SalonPageViewModel.CreateSalonAsync.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            GenerateInviteErrorMessage = exception.Message;
            LogOperationFailed(nameof(GenerateReceptionInviteAsync));
        }
        finally
        {
            IsGeneratingReceptionInvite = false;
        }
    }
}
