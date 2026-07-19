using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Services;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Services;

/// <summary>
/// Drives the service profile panel for one selected service - category,
/// duration, price, and description display, plus assigned specialists.
/// Owned by <see cref="ServicePageViewModel"/>, constructed fresh whenever
/// the selected service changes - same per-selection child-ViewModel
/// pattern <c>Customers.CustomerProfileViewModel</c> established in
/// Phase 10. No editable status/category (unlike Customer/Specialist
/// profiles) - Phase 13 only requested display for those fields, plus the
/// one real write capability: specialist assignment.
/// </summary>
public sealed class ServiceProfileViewModel : ViewModelBase
{
    private readonly string _serviceId;
    private readonly IServiceProfileQueryService _profileQueryService;
    private readonly IServiceCommandService _commandService;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private ServiceDto? _service;
    private string _newSpecialistName = string.Empty;

    public ServiceProfileViewModel(
        string serviceId,
        IServiceProfileQueryService profileQueryService,
        IServiceCommandService commandService)
    {
        _serviceId = serviceId;
        _profileQueryService = profileQueryService;
        _commandService = commandService;

        AssignedSpecialists = new ObservableCollection<AssignedSpecialistDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        AssignSpecialistCommand = new AsyncRelayCommand(_ => AssignSpecialistAsync(), _ => !string.IsNullOrWhiteSpace(NewSpecialistName));
        UnassignSpecialistCommand = new AsyncRelayCommand(parameter => UnassignSpecialistAsync(parameter as AssignedSpecialistDto));

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, same pattern as every
        // other page/profile ViewModel in this app.
        _ = LoadAsync();
    }

    public ObservableCollection<AssignedSpecialistDto> AssignedSpecialists { get; }

    public ICommand LoadCommand { get; }

    public ICommand AssignSpecialistCommand { get; }

    public ICommand UnassignSpecialistCommand { get; }

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

    public ServiceDto? Service
    {
        get => _service;
        private set => SetProperty(ref _service, value);
    }

    public string NewSpecialistName
    {
        get => _newSpecialistName;
        set => SetProperty(ref _newSpecialistName, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var profile = await _profileQueryService.GetProfileAsync(_serviceId).ConfigureAwait(true);

            Service = profile.Service;

            AssignedSpecialists.Clear();
            foreach (var assignment in profile.AssignedSpecialists)
            {
                AssignedSpecialists.Add(assignment);
            }

            State = DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page/profile ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
        }
    }

    private async Task AssignSpecialistAsync()
    {
        await _commandService.AssignSpecialistAsync(_serviceId, NewSpecialistName).ConfigureAwait(true);
        NewSpecialistName = string.Empty;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task UnassignSpecialistAsync(AssignedSpecialistDto? assignment)
    {
        if (assignment is null)
        {
            return;
        }

        await _commandService.UnassignSpecialistAsync(_serviceId, assignment.Id).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
    }
}
