using System.Globalization;
using System.Text;
using System.Windows.Input;
using Rojan.Desktop.Application.Membership;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Organizations;

namespace Rojan.Desktop.Presentation.ViewModels.Membership;

/// <summary>
/// Reception Production Integration: drives AcceptInvitePage - the
/// confirmation-screen flow ROJAN_Backend's own two-step invite design
/// expects (look up, show "join {SalonName} as {Role}?", then accept),
/// shown whenever the signed-in user has no real salon membership yet
/// (owner or accepted invite - see <c>CurrentSessionService.InitializeAsync</c>).
/// Depends only on <see cref="ISalonInviteService"/> (Application) - never
/// the Domain-typed <c>ISalonInviteRepository</c>/<c>IAcceptedMembershipStore</c>
/// directly, same "Presentation only ever reaches Application" boundary
/// <c>SalonPageViewModel</c>/<c>ISalonQueryService</c> already establish
/// (enforced by <c>Rojan.Desktop.ArchitectureTests</c>).
///
/// Two independent error slots, same reasoning as <c>SalonPageViewModel</c>'s
/// <c>CreateErrorMessage</c>: a failed lookup must leave the token field
/// intact for correction without touching a later accept attempt's own
/// error state, and vice versa.
/// </summary>
public sealed class AcceptInviteViewModel : ViewModelBase
{
    // Culture is set once at startup and never changes mid-session (see Strings' own doc
    // comment) - safe to parse once here, same "static readonly CompositeFormat" convention
    // DashboardPageViewModel already establishes for its own resx format strings.
    private static readonly CompositeFormat JoinPromptFormat = CompositeFormat.Parse(Strings.AcceptInvite_JoinPromptFormat);

    private readonly ISalonInviteService _inviteService;
    private readonly ISalonContextService _salonContextService;
    private readonly ICurrentSessionService _currentSessionService;

    private string _token = string.Empty;
    private bool _isLookingUp;
    private string? _lookupErrorMessage;
    private SalonInviteDetailsDto? _details;
    private bool _isAccepting;
    private string? _acceptErrorMessage;
    private bool _isAccepted;

    public AcceptInviteViewModel(
        ISalonInviteService inviteService,
        ISalonContextService salonContextService,
        ICurrentSessionService currentSessionService)
    {
        _inviteService = inviteService;
        _salonContextService = salonContextService;
        _currentSessionService = currentSessionService;

        LookupCommand = new AsyncRelayCommand(_ => LookupAsync(), _ => CanLookup());
        AcceptCommand = new AsyncRelayCommand(_ => AcceptAsync(), _ => CanAccept());
    }

    public ICommand LookupCommand { get; }

    public ICommand AcceptCommand { get; }

    public string Token
    {
        get => _token;
        set
        {
            if (SetProperty(ref _token, value))
            {
                // A changed token invalidates whatever was previously looked up - never accept against a stale confirmation.
                Details = null;
            }
        }
    }

    public bool IsLookingUp
    {
        get => _isLookingUp;
        private set => SetProperty(ref _isLookingUp, value);
    }

    public string? LookupErrorMessage
    {
        get => _lookupErrorMessage;
        private set
        {
            if (SetProperty(ref _lookupErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasLookupError));
            }
        }
    }

    public bool HasLookupError => !string.IsNullOrEmpty(LookupErrorMessage);

    public SalonInviteDetailsDto? Details
    {
        get => _details;
        private set
        {
            if (SetProperty(ref _details, value))
            {
                OnPropertyChanged(nameof(HasDetails));
                OnPropertyChanged(nameof(JoinPrompt));
            }
        }
    }

    /// <summary>Drives whether AcceptInvitePage shows the confirmation panel (salon name/role, Accept button) - the token-entry panel is always visible, same "two panels, one boolean" shape <c>SalonPageViewModel.HasSalon</c>/<c>NeedsSalon</c> already establishes, just with both panels able to be visible at once here (entry field stays usable while confirming).</summary>
    public bool HasDetails => Details is not null;

    public string JoinPrompt => Details is null
        ? string.Empty
        : string.Format(CultureInfo.CurrentCulture, JoinPromptFormat, Details.SalonName, DisplayRole(Details.Role));

    public bool IsAccepting
    {
        get => _isAccepting;
        private set => SetProperty(ref _isAccepting, value);
    }

    public string? AcceptErrorMessage
    {
        get => _acceptErrorMessage;
        private set
        {
            if (SetProperty(ref _acceptErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasAcceptError));
            }
        }
    }

    public bool HasAcceptError => !string.IsNullOrEmpty(AcceptErrorMessage);

    /// <summary>Set once <see cref="AcceptAsync"/> succeeds - the view can bind this to navigate away/collapse itself, same "boolean signal, view decides what to do with it" shape as every other flag here.</summary>
    public bool IsAccepted
    {
        get => _isAccepted;
        private set => SetProperty(ref _isAccepted, value);
    }

    private bool CanLookup() => !IsLookingUp && !string.IsNullOrWhiteSpace(Token);

    private async Task LookupAsync()
    {
        IsLookingUp = true;
        LookupErrorMessage = null;
        Details = null;

        try
        {
            Details = await _inviteService.GetDetailsAsync(Token.Trim()).ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Top-level command boundary: any failure must surface via LookupErrorMessage, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            LookupErrorMessage = exception.Message;
        }
        finally
        {
            IsLookingUp = false;
        }
    }

    private bool CanAccept() => !IsAccepting && Details is not null;

    private async Task AcceptAsync()
    {
        if (Details is null)
        {
            return;
        }

        IsAccepting = true;
        AcceptErrorMessage = null;

        try
        {
            // ISalonInviteService.AcceptAsync already persists the membership locally
            // (see its own doc comment) - nothing further to save here.
            await _inviteService.AcceptAsync(Token.Trim(), Details.SalonName).ConfigureAwait(true);

            // Same "invalidate the cache, then re-run the thing that reads it" pattern
            // SalonCommandService already uses after a successful Create Salon - picks
            // up the brand-new membership across the whole app without a restart.
            _salonContextService.Invalidate();
            await _currentSessionService.InitializeAsync().ConfigureAwait(true);

            IsAccepted = true;
        }
#pragma warning disable CA1031 // Top-level command boundary: any failure must surface via AcceptErrorMessage, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            AcceptErrorMessage = exception.Message;
        }
        finally
        {
            IsAccepting = false;
        }
    }

    private static string DisplayRole(string backendRole) => backendRole switch
    {
        "RECEPTIONIST" => Strings.AcceptInvite_RoleReceptionist,
        "MANAGER" => Strings.AcceptInvite_RoleManager,
        _ => backendRole,
    };
}
