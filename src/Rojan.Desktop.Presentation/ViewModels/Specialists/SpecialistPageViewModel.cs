using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Specialists;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Specialists;

/// <summary>
/// Drives SpecialistPage - the specialist directory/search on the left,
/// and the selected specialist's <see cref="SpecialistProfileViewModel"/>
/// (status + skills) on the right. Depends only on Application services
/// (<see cref="ISpecialistQueryService"/>, <see cref="ISpecialistProfileQueryService"/>,
/// <see cref="ISpecialistCommandService"/>), consistent with Presentation
/// never reaching past Application into Domain/Infrastructure. Reuses
/// <see cref="DashboardState"/> rather than a duplicate enum, same
/// reasoning as every other page ViewModel in this app.
/// </summary>
public sealed class SpecialistPageViewModel : ViewModelBase
{
    private readonly ISpecialistQueryService _queryService;
    private readonly ISpecialistProfileQueryService _profileQueryService;
    private readonly ISpecialistCommandService _commandService;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private string _searchText = string.Empty;
    private SpecialistDto? _selectedSpecialist;
    private SpecialistProfileViewModel? _profile;
    private string _newSpecialistFullName = string.Empty;
    private string _newSpecialistTitle = string.Empty;
    private string _newSpecialistEmail = string.Empty;
    private string _newSpecialistPhone = string.Empty;

    public SpecialistPageViewModel(
        ISpecialistQueryService queryService,
        ISpecialistProfileQueryService profileQueryService,
        ISpecialistCommandService commandService)
    {
        _queryService = queryService;
        _profileQueryService = profileQueryService;
        _commandService = commandService;

        Specialists = new ObservableCollection<SpecialistDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
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
                _ = SearchAsync(value);
            }
        }
    }

    public SpecialistDto? SelectedSpecialist
    {
        get => _selectedSpecialist;
        set
        {
            if (SetProperty(ref _selectedSpecialist, value))
            {
                Profile = value is null
                    ? null
                    : new SpecialistProfileViewModel(value.Id, _profileQueryService, _commandService);
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

        try
        {
            var specialists = await _queryService.GetSpecialistsAsync().ConfigureAwait(true);
            ReplaceAll(specialists);

            State = specialists.Count == 0
                ? DashboardState.Empty
                : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - this is the one place a broad catch is the correct behavior, not a code smell.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
        }
    }

    /// <summary>
    /// Runs the search through <see cref="ISpecialistQueryService.SearchSpecialistsAsync"/>
    /// rather than filtering a client-side cache - same reasoning as
    /// <c>Customers.CustomerPageViewModel.SearchAsync</c>. Guards against
    /// out-of-order completions: if the user kept typing after this call
    /// started, <paramref name="searchText"/> no longer matches
    /// <see cref="SearchText"/> by the time the result arrives, and the
    /// stale result is discarded.
    /// </summary>
    private async Task SearchAsync(string searchText)
    {
        try
        {
            var results = await _queryService.SearchSpecialistsAsync(searchText).ConfigureAwait(true);
            if (!string.Equals(searchText, SearchText, StringComparison.Ordinal))
            {
                return;
            }

            ReplaceAll(results);
        }
#pragma warning disable CA1031 // Same top-level boundary reasoning as LoadAsync - a failed search must surface as the Error state, not crash the page.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            if (string.Equals(searchText, SearchText, StringComparison.Ordinal))
            {
                ErrorMessage = exception.Message;
                State = DashboardState.Error;
            }
        }
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
