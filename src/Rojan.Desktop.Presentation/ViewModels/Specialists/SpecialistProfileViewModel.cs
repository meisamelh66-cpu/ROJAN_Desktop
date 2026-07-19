using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Specialists;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Specialists;

/// <summary>
/// Drives the specialist profile panel for one selected specialist -
/// status display/edit and skills. Owned by <see cref="SpecialistPageViewModel"/>,
/// constructed fresh whenever the selected specialist changes - same
/// per-selection child-ViewModel pattern <c>Customers.CustomerProfileViewModel</c>
/// established in Phase 10. No notes/timeline (unlike Customer 360) - not
/// requested for Specialists, keeping this foundation-scoped.
/// </summary>
public sealed class SpecialistProfileViewModel : ViewModelBase
{
    private readonly string _specialistId;
    private readonly ISpecialistProfileQueryService _profileQueryService;
    private readonly ISpecialistCommandService _commandService;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private SpecialistDto? _specialist;
    private string _newSkillText = string.Empty;
    private SpecialistStatus _editableStatus;

    public SpecialistProfileViewModel(
        string specialistId,
        ISpecialistProfileQueryService profileQueryService,
        ISpecialistCommandService commandService)
    {
        _specialistId = specialistId;
        _profileQueryService = profileQueryService;
        _commandService = commandService;

        Skills = new ObservableCollection<SpecialistSkillDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        AddSkillCommand = new AsyncRelayCommand(_ => AddSkillAsync(), _ => !string.IsNullOrWhiteSpace(NewSkillText));
        RemoveSkillCommand = new AsyncRelayCommand(parameter => RemoveSkillAsync(parameter as SpecialistSkillDto));
        SaveChangesCommand = new AsyncRelayCommand(_ => SaveChangesAsync());

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, same pattern as every
        // other page/profile ViewModel in this app.
        _ = LoadAsync();
    }

    public ObservableCollection<SpecialistSkillDto> Skills { get; }

    public IReadOnlyList<SpecialistStatus> AvailableStatuses { get; } = Enum.GetValues<SpecialistStatus>();

    public ICommand LoadCommand { get; }

    public ICommand AddSkillCommand { get; }

    public ICommand RemoveSkillCommand { get; }

    public ICommand SaveChangesCommand { get; }

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

    public SpecialistDto? Specialist
    {
        get => _specialist;
        private set => SetProperty(ref _specialist, value);
    }

    public string NewSkillText
    {
        get => _newSkillText;
        set => SetProperty(ref _newSkillText, value);
    }

    /// <summary>Bound by the status ComboBox - a local edit buffer, synced from <see cref="Specialist"/> on every load so it always starts matching the persisted value.</summary>
    public SpecialistStatus EditableStatus
    {
        get => _editableStatus;
        set => SetProperty(ref _editableStatus, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var profile = await _profileQueryService.GetProfileAsync(_specialistId).ConfigureAwait(true);

            Specialist = profile.Specialist;
            EditableStatus = profile.Specialist.Status;

            Skills.Clear();
            foreach (var skill in profile.Skills)
            {
                Skills.Add(skill);
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

    private async Task AddSkillAsync()
    {
        await _commandService.AddSkillAsync(_specialistId, NewSkillText).ConfigureAwait(true);
        NewSkillText = string.Empty;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task RemoveSkillAsync(SpecialistSkillDto? skill)
    {
        if (skill is null)
        {
            return;
        }

        await _commandService.RemoveSkillAsync(_specialistId, skill.Id).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task SaveChangesAsync()
    {
        if (Specialist is null)
        {
            return;
        }

        var request = new UpdateSpecialistRequest(
            Specialist.Id,
            Specialist.FullName,
            Specialist.Title,
            Specialist.Email,
            Specialist.Phone,
            EditableStatus,
            Specialist.Bio);

        await _commandService.UpdateSpecialistAsync(request).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
    }
}
