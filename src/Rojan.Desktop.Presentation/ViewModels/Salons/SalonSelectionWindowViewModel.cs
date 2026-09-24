using System.Windows.Input;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Salons;

/// <summary>
/// PASS D6 (Active Salon Context Correctness): <c>SalonSelectionWindow</c>'s DataContext - shown
/// once, before <c>MainWindow</c>, only when the signed-in account has more than one real candidate
/// salon and none is already the active context (<c>Shell.App.OnStartup</c>'s own gate, mirroring
/// the existing Login gate's shape exactly). Constructed via <c>new</c> by its opener (same
/// "constructed-via-new-by-its-opener" shape <c>MainWindowViewModel.OpenHelpAsync</c>/
/// <c>OpenCommandPaletteAsync</c> already establish), not DI-resolved - <see cref="Candidates"/> is
/// a real, runtime-only value (this account's actual salon-access response) no constructor-injected
/// dependency could supply.
/// </summary>
public sealed class SalonSelectionWindowViewModel : ViewModelBase
{
    private readonly ISalonContextService _salonContextService;
    private bool _isBusy;
    private string? _errorMessage;

    public SalonSelectionWindowViewModel(ISalonContextService salonContextService, IReadOnlyList<SalonContext> candidates)
    {
        _salonContextService = salonContextService;
        Candidates = candidates;
        SelectCommand = new AsyncRelayCommand(parameter => SelectAsync((SalonContext)parameter!));
    }

    /// <summary>The real salons this account has access to - never a fabricated or hardcoded list, exactly what <see cref="ISalonContextService.GetAccessSummaryAsync"/> resolved.</summary>
    public IReadOnlyList<SalonContext> Candidates { get; }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ICommand SelectCommand { get; }

    /// <summary>Raised once a real, validated selection succeeds - <c>SalonSelectionWindow</c> subscribes to this to know when to close, same shape as <see cref="Security.LoginWindowViewModel.SignedIn"/>.</summary>
    public event EventHandler? Selected;

    /// <summary>
    /// Re-validates against the real candidate list via <see cref="ISalonContextService.SelectSalonAsync"/>
    /// rather than trusting <paramref name="candidate"/> blindly - it is one of <see cref="Candidates"/>
    /// by construction (bound from this same list), but the service is the single source of truth for
    /// what is actually still valid, and never silently assumed to still match a stale in-memory copy.
    /// </summary>
    private async Task SelectAsync(SalonContext candidate)
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var succeeded = await _salonContextService.SelectSalonAsync(candidate.SalonId).ConfigureAwait(true);
            if (succeeded)
            {
                Selected?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ErrorMessage = Strings.SalonSelection_Error_InvalidSelection;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
