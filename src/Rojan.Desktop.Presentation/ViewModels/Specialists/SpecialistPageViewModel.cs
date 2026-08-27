using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Intelligence;
using Rojan.Desktop.Application.Services;
using Rojan.Desktop.Application.Specialists;
using Rojan.Desktop.Application.Specialists.Schedule;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Specialists;

/// <summary>
/// Drives SpecialistPage - the specialist directory/search on the left,
/// and the selected specialist's <see cref="SpecialistProfileViewModel"/>
/// (status + skills + Sprint 5 Commit 5C's performance intelligence) on
/// the right. Depends only on Application services (<see cref="ISpecialistQueryService"/>,
/// <see cref="ISpecialistProfileQueryService"/>, <see cref="ISpecialistCommandService"/>,
/// <see cref="IIntelligenceEngine"/>), consistent with Presentation never
/// reaching past Application into Domain/Infrastructure. Reuses
/// <see cref="DashboardState"/> rather than a duplicate enum, same
/// reasoning as every other page ViewModel in this app.
///
/// Sprint 5 Commit 4 (Premium Specialist Search &amp; Profile foundation):
/// <see cref="SearchText"/>/<see cref="StatusFilter"/>/<see cref="SelectedSkill"/>
/// are combined into one <see cref="SpecialistSearchFilter"/> and run
/// through <see cref="ISpecialistQueryService.SearchSpecialistsAsync(SpecialistSearchFilter, CancellationToken)"/> -
/// every load (including the initial one) goes through this same method
/// now, not a separate <c>GetSpecialistsAsync</c>/<c>SearchSpecialistsAsync(string)</c>
/// path, the same unification Customers'/Bookings'/Services' own search
/// commits already established. An all-default filter is equivalent to
/// the old unfiltered <c>GetSpecialistsAsync</c> call - see
/// <see cref="SpecialistSearchFilter"/>'s own doc comment. Same
/// <c>_filterVersion</c> staleness-guard pattern as every other page
/// ViewModel with combinable filters.
/// </summary>
public sealed class SpecialistPageViewModel : ViewModelBase
{
    private readonly ISpecialistQueryService _queryService;
    private readonly ISpecialistProfileQueryService _profileQueryService;
    private readonly ISpecialistCommandService _commandService;
    private readonly IIntelligenceEngine _intelligenceEngine;
    private readonly IServiceQueryService _serviceQueryService;
    private readonly ISpecialistScheduleQueryService _scheduleQueryService;
    private readonly ISpecialistScheduleCommandService _scheduleCommandService;
    private readonly ILogger<SpecialistScheduleViewModel>? _scheduleLogger;
    private readonly ILogger<SpecialistAvailabilityViewModel>? _availabilityLogger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private string _searchText = string.Empty;
    private SpecialistStatus? _statusFilter;
    private string _selectedSkill = string.Empty;
    private SpecialistDto? _selectedSpecialist;
    private SpecialistProfileViewModel? _profile;
    private string _newSpecialistFullName = string.Empty;
    private string _newSpecialistTitle = string.Empty;
    private string _newSpecialistEmail = string.Empty;
    private string _newSpecialistPhone = string.Empty;

    /// <summary>Incremented on every filter/load-triggering change - see <c>Bookings.BookingPageViewModel</c>'s field of the same name for the full reasoning.</summary>
    private int _filterVersion;

    public SpecialistPageViewModel(
        ISpecialistQueryService queryService,
        ISpecialistProfileQueryService profileQueryService,
        ISpecialistCommandService commandService,
        IIntelligenceEngine intelligenceEngine,
        IServiceQueryService serviceQueryService,
        ISpecialistScheduleQueryService scheduleQueryService,
        ISpecialistScheduleCommandService scheduleCommandService,
        ILogger<SpecialistScheduleViewModel>? scheduleLogger = null,
        ILogger<SpecialistAvailabilityViewModel>? availabilityLogger = null)
    {
        _queryService = queryService;
        _profileQueryService = profileQueryService;
        _scheduleLogger = scheduleLogger;
        _availabilityLogger = availabilityLogger;
        _commandService = commandService;
        _intelligenceEngine = intelligenceEngine;
        _serviceQueryService = serviceQueryService;
        _scheduleQueryService = scheduleQueryService;
        _scheduleCommandService = scheduleCommandService;

        Specialists = new ObservableCollection<SpecialistDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        SearchCommand = new AsyncRelayCommand(_ => LoadAsync());
        ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
        CreateSpecialistCommand = new AsyncRelayCommand(
            _ => CreateSpecialistAsync(),
            _ => !string.IsNullOrWhiteSpace(NewSpecialistFullName));

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, so there is nothing
        // left that could become an unobserved task exception.
        _ = LoadAsync();
    }

    public ObservableCollection<SpecialistDto> Specialists { get; }

    /// <summary>Re-runs the load - bound as the Retry action on DashboardWidget's Error state.</summary>
    public ICommand LoadCommand { get; }

    /// <summary>Explicit re-run of the current filter combination - the reactive filter-property setters already trigger a load on every change, so this exists for an explicit Search action (button/Enter key) a future UI pass may wire up.</summary>
    public ICommand SearchCommand { get; }

    /// <summary>Resets every filter property to its default (empty/null) and reloads - equivalent to a freshly-opened, unfiltered directory view.</summary>
    public ICommand ClearFiltersCommand { get; }

    public ICommand CreateSpecialistCommand { get; }

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
                _ = LoadAsync();
            }
        }
    }

    /// <summary>Null means "every status" - the first entry of <see cref="AvailableStatusOptions"/>.</summary>
    public SpecialistStatus? StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                _ = LoadAsync();
            }
        }
    }

    /// <summary>Filters to specialists with a matching skill name - see <see cref="SpecialistSearchFilter"/>'s own doc comment for why this stands in for a "service filter" no existing query can answer.</summary>
    public string SelectedSkill
    {
        get => _selectedSkill;
        set
        {
            if (SetProperty(ref _selectedSkill, value))
            {
                _ = LoadAsync();
            }
        }
    }

    /// <summary>Bindable options for the status filter ComboBox - leads with <c>null</c> ("every status") followed by every real <see cref="SpecialistStatus"/> value.</summary>
    public IReadOnlyList<SpecialistStatus?> AvailableStatusOptions { get; } =
        new SpecialistStatus?[] { null }.Concat(Enum.GetValues<SpecialistStatus>().Cast<SpecialistStatus?>()).ToList();

    public SpecialistDto? SelectedSpecialist
    {
        get => _selectedSpecialist;
        set
        {
            if (SetProperty(ref _selectedSpecialist, value))
            {
                if (Profile is not null)
                {
                    Profile.SpecialistUpdated -= OnProfileSpecialistUpdated;
                }

                Profile = value is null
                    ? null
                    : new SpecialistProfileViewModel(value.Id, _profileQueryService, _commandService, _intelligenceEngine, _serviceQueryService, _scheduleQueryService, _scheduleCommandService, _scheduleLogger, _availabilityLogger);

                if (Profile is not null)
                {
                    Profile.SpecialistUpdated += OnProfileSpecialistUpdated;
                }
            }
        }
    }

    /// <summary>Profile for <see cref="SelectedSpecialist"/> - null when nothing is selected.</summary>
    public SpecialistProfileViewModel? Profile
    {
        get => _profile;
        private set => SetProperty(ref _profile, value);
    }

    public string NewSpecialistFullName
    {
        get => _newSpecialistFullName;
        set => SetProperty(ref _newSpecialistFullName, value);
    }

    public string NewSpecialistTitle
    {
        get => _newSpecialistTitle;
        set => SetProperty(ref _newSpecialistTitle, value);
    }

    public string NewSpecialistEmail
    {
        get => _newSpecialistEmail;
        set => SetProperty(ref _newSpecialistEmail, value);
    }

    public string NewSpecialistPhone
    {
        get => _newSpecialistPhone;
        set => SetProperty(ref _newSpecialistPhone, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        var requestVersion = ++_filterVersion;

        try
        {
            var specialists = await _queryService.SearchSpecialistsAsync(BuildFilter()).ConfigureAwait(true);

            if (requestVersion != _filterVersion)
            {
                // A newer filter change (or another reload) started after
                // this one - its result will win instead, so applying this
                // now-stale response would flash outdated data.
                return;
            }

            ReplaceAll(specialists);

            State = specialists.Count == 0
                ? DashboardState.Empty
                : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - this is the one place a broad catch is the correct behavior, not a code smell.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            if (requestVersion == _filterVersion)
            {
                ErrorMessage = exception.Message;
                State = DashboardState.Error;
            }
        }
    }

    private SpecialistSearchFilter BuildFilter() => new(
        SearchText: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
        Status: StatusFilter,
        Skill: string.IsNullOrWhiteSpace(SelectedSkill) ? null : SelectedSkill);

    private void ClearFilters()
    {
        _searchText = string.Empty;
        _statusFilter = null;
        _selectedSkill = string.Empty;

        // Raise every property-changed notification once, then reload once - setting each property
        // individually would fire LoadAsync repeatedly (once per property) for a single user action.
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(StatusFilter));
        OnPropertyChanged(nameof(SelectedSkill));

        _ = LoadAsync();
    }

    private async Task CreateSpecialistAsync()
    {
        var request = new CreateSpecialistRequest(NewSpecialistFullName, NewSpecialistTitle, NewSpecialistEmail, NewSpecialistPhone, string.Empty);
        var created = await _commandService.CreateSpecialistAsync(request).ConfigureAwait(true);

        NewSpecialistFullName = string.Empty;
        NewSpecialistTitle = string.Empty;
        NewSpecialistEmail = string.Empty;
        NewSpecialistPhone = string.Empty;

        await LoadAsync().ConfigureAwait(true);
        SelectedSpecialist = Specialists.FirstOrDefault(specialist => specialist.Id == created.Id);
    }

    /// <summary>
    /// Specialist Deactivation Wiring: reloads the directory after the
    /// selected specialist's profile reports a successful save, so a
    /// just-deactivated specialist's status is never left stale in this
    /// list. Re-selects by id afterward - same "reload, then re-select by
    /// the id that changed" shape <see cref="CreateSpecialistAsync"/>
    /// already established, needed because <see cref="SpecialistDto"/> is a
    /// record: the pre-reload <see cref="SelectedSpecialist"/> still carries
    /// the old status, so it no longer equals its own freshly-reloaded
    /// entry and <see cref="ReplaceAll"/>'s "selection no longer present"
    /// fallback would otherwise jump the selection to the first specialist
    /// in the list instead of keeping the one the user was just working on.
    /// Safe fire-and-forget for the same reason the constructor's own
    /// initial <see cref="LoadAsync"/> call is - it catches every failure
    /// internally.
    /// </summary>
    private async void OnProfileSpecialistUpdated(object? sender, EventArgs e)
    {
        var updatedId = SelectedSpecialist?.Id;
        await LoadAsync().ConfigureAwait(true);

        if (updatedId is not null)
        {
            SelectedSpecialist = Specialists.FirstOrDefault(specialist => specialist.Id == updatedId);
        }
    }

    private void ReplaceAll(IReadOnlyList<SpecialistDto> specialists)
    {
        Specialists.Clear();
        foreach (var specialist in specialists)
        {
            Specialists.Add(specialist);
        }

        if (SelectedSpecialist is null || !Specialists.Contains(SelectedSpecialist))
        {
            SelectedSpecialist = Specialists.Count > 0 ? Specialists[0] : null;
        }
    }
}
