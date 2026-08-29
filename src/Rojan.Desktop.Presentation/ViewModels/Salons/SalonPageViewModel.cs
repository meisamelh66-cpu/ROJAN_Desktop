using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Salons;

/// <summary>
/// Phase 1.2 Owner App Create Salon Flow. Drives SalonPage - unlike every
/// other module's page ViewModel, this one shows *either* a read-only
/// summary of the signed-in owner's one salon *or* a create form, never
/// both, depending on <see cref="HasSalon"/>. Deliberately does not use
/// <see cref="DashboardState.Empty"/> for "no salon yet" - <c>DashboardWidget</c>
/// replaces its content with a generic empty-state message for that state
/// (see that control's own doc comment), which would swallow the create
/// form entirely. <see cref="State"/> here only ever reflects the *load*
/// outcome (Loading/Loaded/Error) - "loaded, and it turned out there's no
/// salon yet" is still <see cref="DashboardState.Loaded"/>, with
/// <see cref="HasSalon"/> as the separate signal the view switches its two
/// content panels on, the same "boolean flag picks between two panels"
/// shape <c>CalendarPageViewModel.IsDayView</c>/<c>IsWeekView</c> already
/// establishes.
///
/// The create form's own submit failure (e.g. a 400 from backend
/// validation) is tracked by <see cref="CreateErrorMessage"/>, deliberately
/// separate from <see cref="ErrorMessage"/>/<see cref="State"/> - a failed
/// create attempt must leave the form visible with the user's typed input
/// intact for correction, not flip the whole page into the Error state.
/// </summary>
public sealed partial class SalonPageViewModel : ViewModelBase
{
    private readonly ISalonQueryService _queryService;
    private readonly ISalonCommandService _commandService;
    private readonly ILogger<SalonPageViewModel> _logger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private SalonDto? _salon;

    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _address = string.Empty;
    private bool _isCreating;
    private string? _createErrorMessage;

    public SalonPageViewModel(ISalonQueryService queryService, ISalonCommandService commandService, ILogger<SalonPageViewModel>? logger = null)
    {
        _queryService = queryService;
        _commandService = commandService;
        _logger = logger ?? NullLogger<SalonPageViewModel>.Instance;

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        CreateSalonCommand = new AsyncRelayCommand(_ => CreateSalonAsync(), _ => CanCreate());

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, same reasoning as every
        // other page ViewModel's constructor-time load.
        _ = LoadAsync();
    }

    /// <summary>Re-runs the load - bound as the Retry action on DashboardWidget's Error state.</summary>
    public ICommand LoadCommand { get; }

    public ICommand CreateSalonCommand { get; }

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

    public SalonDto? Salon
    {
        get => _salon;
        private set
        {
            if (SetProperty(ref _salon, value))
            {
                OnPropertyChanged(nameof(HasSalon));
                OnPropertyChanged(nameof(NeedsSalon));
            }
        }
    }

    /// <summary>Drives which of the two content panels SalonPage shows - see this class's own doc comment for why this is separate from <see cref="State"/>.</summary>
    public bool HasSalon => Salon is not null;

    /// <summary>The exact inverse of <see cref="HasSalon"/> - a distinct property (not a converter-level negation) so the view's two-panel Visibility bindings stay simple, direct bool-to-Visibility conversions.</summary>
    public bool NeedsSalon => Salon is null;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public bool IsCreating
    {
        get => _isCreating;
        private set => SetProperty(ref _isCreating, value);
    }

    public string? CreateErrorMessage
    {
        get => _createErrorMessage;
        private set
        {
            if (SetProperty(ref _createErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasCreateError));
            }
        }
    }

    /// <summary>Direct bool for the view's error-text Visibility binding - <see cref="Rojan.Desktop.Presentation.Converters.BoolToVisibilityConverter"/> only ever treats a boxed <see langword="true"/> as visible, so binding it to a string directly would always collapse regardless of content.</summary>
    public bool HasCreateError => !string.IsNullOrEmpty(CreateErrorMessage);

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            Salon = await _queryService.GetMySalonAsync().ConfigureAwait(true);
            State = DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception)
#pragma warning restore CA1031
        {
            ErrorMessage = Strings.Common_ActionFailedMessage;
            State = DashboardState.Error;
            LogOperationFailed(nameof(LoadAsync));
        }
    }

    // Security: logs the operation name only - never the exception, its message,
    // salon contact details (name/phone/email/address), or any backend response.
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Salon page operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);

    private bool CanCreate() =>
        !IsCreating
        && !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrWhiteSpace(Phone)
        && !string.IsNullOrWhiteSpace(Address);

    private async Task CreateSalonAsync()
    {
        IsCreating = true;
        CreateErrorMessage = null;

        try
        {
            var command = new CreateSalonCommand(
                Name.Trim(),
                NullIfEmpty(Description)?.Trim(),
                Phone.Trim(),
                NullIfEmpty(Email)?.Trim(),
                Address.Trim());

            Salon = await _commandService.CreateSalonAsync(command).ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Top-level command boundary: any failure must surface via CreateErrorMessage, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception)
#pragma warning restore CA1031
        {
            CreateErrorMessage = Strings.Common_ActionFailedMessage;
            LogOperationFailed(nameof(CreateSalonAsync));
        }
        finally
        {
            IsCreating = false;
        }
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
