using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Specialists.Schedule;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Specialists;

/// <summary>
/// Phase 7.2.6 Shift Engine UI Activation - the read-only availability
/// view: what Reception sees while helping a customer, or a specialist
/// reviewing their own configured schedule. This Desktop app has no real
/// "Customer" session/role at all (confirmed against <c>WorkspaceRole</c> -
/// customers never log into this app; only Owner/Manager/Reception/
/// Specialist/etc. do), so this is deliberately named around what it
/// actually is - a read-only availability display - rather than
/// implying a customer-facing login this app doesn't have. It is the
/// Presentation answer to the roadmap's "Customer availability UI" line:
/// the availability information a customer's booking depends on, shown
/// read-only to the staff member acting on the customer's behalf.
///
/// Query-only, no permission gate - matches <see cref="ISpecialistScheduleQueryService"/>'s
/// own "reads are not permission-gated" convention (same as every other
/// <c>I*QueryService</c> in this codebase). No command, no mutation, no
/// input buffer - this view cannot change anything, by construction, not
/// by a runtime check.
///
/// Phase 7.4.1 Production Hardening: the caught load failure is now also
/// logged (<see cref="LogLoadFailed"/>) - see <see cref="SpecialistScheduleViewModel"/>'s
/// own doc comment for the full reasoning, including why <see cref="ILogger{T}"/>
/// is optional here too.
/// </summary>
public sealed partial class SpecialistAvailabilityViewModel : ViewModelBase
{
    private readonly string _specialistId;
    private readonly ISpecialistScheduleQueryService _queryService;
    private readonly ILogger<SpecialistAvailabilityViewModel> _logger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;

    public SpecialistAvailabilityViewModel(string specialistId, ISpecialistScheduleQueryService queryService, ILogger<SpecialistAvailabilityViewModel>? logger = null)
    {
        _specialistId = specialistId;
        _queryService = queryService;
        _logger = logger ?? NullLogger<SpecialistAvailabilityViewModel>.Instance;

        WeeklyAvailability = new ObservableCollection<WeeklyAvailabilityDto>();
        Overrides = new ObservableCollection<ScheduleOverrideDto>();
        Leave = new ObservableCollection<SpecialistLeaveDto>();
        Blocks = new ObservableCollection<SpecialistBlockDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());

        // Safe fire-and-forget: LoadAsync catches every failure internally and represents it via
        // State/ErrorMessage, same pattern as every other page/profile ViewModel in this app.
        _ = LoadAsync();
    }

    public ObservableCollection<WeeklyAvailabilityDto> WeeklyAvailability { get; }

    public ObservableCollection<ScheduleOverrideDto> Overrides { get; }

    public ObservableCollection<SpecialistLeaveDto> Leave { get; }

    public ObservableCollection<SpecialistBlockDto> Blocks { get; }

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

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var availability = await _queryService.GetWeeklyAvailabilityAsync(_specialistId).ConfigureAwait(true);
            var overrides = await _queryService.GetOverridesAsync(_specialistId).ConfigureAwait(true);
            var leave = await _queryService.GetLeaveAsync(_specialistId).ConfigureAwait(true);
            var blocks = await _queryService.GetBlocksAsync(_specialistId).ConfigureAwait(true);

            Replace(WeeklyAvailability, availability);
            Replace(Overrides, overrides);
            Replace(Leave, leave);
            Replace(Blocks, blocks);

            State = availability.Count == 0 && overrides.Count == 0 && leave.Count == 0 && blocks.Count == 0
                ? DashboardState.Empty
                : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page/profile ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
            LogLoadFailed(nameof(LoadAsync));
        }
    }

    // Operation name only: neither the caught exception nor the specialist id is
    // passed to the logger (Phase 8.15+ security rule - no backend bodies, no identifiers).
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Specialist availability load failed. Operation={Operation}")]
    private partial void LogLoadFailed(string operation);

    private static void Replace<T>(ObservableCollection<T> collection, IReadOnlyList<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
