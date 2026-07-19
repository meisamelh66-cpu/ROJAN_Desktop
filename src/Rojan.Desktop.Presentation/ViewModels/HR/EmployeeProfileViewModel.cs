using System.Windows.Input;
using Rojan.Desktop.Application.HR;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.HR;

/// <summary>
/// Drives the employee profile panel for one selected employee - detail
/// info, recent attendance, upcoming shifts, leave requests, and recent
/// commissions, plus lifecycle actions (activate/deactivate/suspend).
/// Owned by <see cref="HrPageViewModel"/>, constructed fresh whenever the
/// selected employee changes - same per-selection child-ViewModel
/// pattern <c>Customers.CustomerProfileViewModel</c> established in
/// Phase 10.
/// </summary>
public sealed class EmployeeProfileViewModel : ViewModelBase
{
    private readonly string _employeeId;
    private readonly IEmployeeQueryService _queryService;
    private readonly IEmployeeCommandService _commandService;
    private readonly Action? _onChanged;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private EmployeeProfileDto? _profile;

    public EmployeeProfileViewModel(string employeeId, IEmployeeQueryService queryService, IEmployeeCommandService commandService, Action? onChanged = null)
    {
        _employeeId = employeeId;
        _queryService = queryService;
        _commandService = commandService;
        _onChanged = onChanged;

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        ActivateCommand = new AsyncRelayCommand(_ => ActivateAsync());
        DeactivateCommand = new AsyncRelayCommand(_ => DeactivateAsync());
        SuspendCommand = new AsyncRelayCommand(_ => SuspendAsync());

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, same pattern as every
        // other page/profile ViewModel in this app.
        _ = LoadAsync();
    }

    public ICommand LoadCommand { get; }

    public ICommand ActivateCommand { get; }

    public ICommand DeactivateCommand { get; }

    public ICommand SuspendCommand { get; }

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

    public EmployeeProfileDto? Profile
    {
        get => _profile;
        private set => SetProperty(ref _profile, value);
    }

    public async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            Profile = await _queryService.GetEmployeeProfileAsync(_employeeId).ConfigureAwait(true);
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

    private async Task ActivateAsync()
    {
        await _commandService.ActivateEmployeeAsync(_employeeId).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
        _onChanged?.Invoke();
    }

    private async Task DeactivateAsync()
    {
        await _commandService.DeactivateEmployeeAsync(_employeeId).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
        _onChanged?.Invoke();
    }

    private async Task SuspendAsync()
    {
        await _commandService.SuspendEmployeeAsync(_employeeId).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
        _onChanged?.Invoke();
    }
}
