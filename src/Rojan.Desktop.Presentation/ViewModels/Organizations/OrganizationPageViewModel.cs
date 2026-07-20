using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Organizations;

/// <summary>
/// Drives the Organization &amp; Branches page - the Enterprise
/// Multi-Branch platform's own administration surface: Organizations
/// (list/create), Branches (list/create, scoped to the selected
/// organization - the concrete "Repository Filtering"/"no cross-branch
/// data leakage" demonstration), Branch Settings (business hours/working
/// days/VAT/receipt/appointment rules/notifications for the selected
/// branch), Permissions (a read-only <see cref="WorkspaceRole"/> -&gt;
/// <see cref="Permission"/> reference grid), and Session (the current
/// organization/branch/role, plus a role switcher - the Branch Switcher
/// itself lives in the Shell header, see <c>Shell.MainWindowViewModel</c>).
/// Every load path is async, non-blocking, and exposes Loading/Empty/
/// Error/Retry via <see cref="State"/> (rendered by the shared
/// <c>DashboardWidget</c> control) plus an explicit Refresh command and a
/// Last Updated timestamp, per this phase's "every new page must include"
/// requirement.
/// </summary>
public sealed class OrganizationPageViewModel : ViewModelBase
{
    private readonly IOrganizationQueryService _queryService;
    private readonly IOrganizationCommandService _commandService;
    private readonly IPermissionEngine _permissionEngine;
    private readonly Rojan.Desktop.Presentation.Organizations.ICurrentSessionService _currentSessionService;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private OrganizationSection _selectedSection = OrganizationSection.Organizations;
    private DateTimeOffset? _lastUpdated;
    private string _statusMessage = string.Empty;

    private OrganizationDto? _selectedOrganization;
    private BranchDto? _selectedBranch;
    private BranchSettingsDto? _currentBranchSettings;

    private string _newOrgName = string.Empty;
    private string _newOrgLegalName = string.Empty;
    private string _newOrgTaxInformation = string.Empty;
    private SubscriptionPlan _newOrgSubscription = SubscriptionPlan.Trial;

    private string _newBranchName = string.Empty;
    private string _newBranchCode = string.Empty;
    private string _newBranchAddress = string.Empty;
    private string _newBranchPhone = string.Empty;
    private string _newBranchEmail = string.Empty;
    private string _newBranchManager = string.Empty;
    private string _newBranchTimeZone = string.Empty;
    private string _newBranchCurrency = string.Empty;

    private string _settingsOpenTime = "09:00";
    private string _settingsCloseTime = "18:00";
    private bool _settingsMonday;
    private bool _settingsTuesday;
    private bool _settingsWednesday;
    private bool _settingsThursday;
    private bool _settingsFriday;
    private bool _settingsSaturday;
    private bool _settingsSunday;
    private decimal _settingsVatPercentage;
    private string _settingsReceiptHeader = string.Empty;
    private string _settingsReceiptFooter = string.Empty;
    private bool _settingsShowLogo;
    private int _settingsMinNoticeHours;
    private int _settingsMaxAdvanceBookingDays;
    private bool _settingsAllowSameDayBooking;
    private bool _settingsEmailEnabled;
    private bool _settingsSmsEnabled;
    private int _settingsReminderHoursBeforeAppointment;

    private WorkspaceRole _selectedRoleToSwitchTo;

    public OrganizationPageViewModel(
        IOrganizationQueryService queryService,
        IOrganizationCommandService commandService,
        IPermissionEngine permissionEngine,
        Rojan.Desktop.Presentation.Organizations.ICurrentSessionService currentSessionService)
    {
        _queryService = queryService;
        _commandService = commandService;
        _permissionEngine = permissionEngine;
        _currentSessionService = currentSessionService;

        Organizations = new ObservableCollection<OrganizationDto>();
        Branches = new ObservableCollection<BranchDto>();
        PermissionMatrix = new ObservableCollection<PermissionMatrixRow>(
            Enum.GetValues<WorkspaceRole>().Select(role => new PermissionMatrixRow(role, string.Join(", ", _permissionEngine.GetPermissions(role).OrderBy(p => p.ToString())))));
        AvailableRoles = Enum.GetValues<WorkspaceRole>();
        AvailableSubscriptionPlans = Enum.GetValues<SubscriptionPlan>();
        _selectedRoleToSwitchTo = _currentSessionService.CurrentRole;

        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync());
        SelectSectionCommand = new RelayCommand(section => SelectedSection = (OrganizationSection)section!);
        CreateOrganizationCommand = new AsyncRelayCommand(_ => CreateOrganizationAsync(), _ => !string.IsNullOrWhiteSpace(NewOrgName));
        CreateBranchCommand = new AsyncRelayCommand(_ => CreateBranchAsync(), _ => SelectedOrganization is not null && !string.IsNullOrWhiteSpace(NewBranchName));
        SaveBranchSettingsCommand = new AsyncRelayCommand(_ => SaveBranchSettingsAsync(), _ => SelectedBranch is not null);
        SwitchRoleCommand = new AsyncRelayCommand(_ => SwitchRoleAsync());

        _ = LoadAsync();
    }

    public ObservableCollection<OrganizationDto> Organizations { get; }

    public ObservableCollection<BranchDto> Branches { get; }

    public ObservableCollection<PermissionMatrixRow> PermissionMatrix { get; }

    public IReadOnlyList<WorkspaceRole> AvailableRoles { get; }

    public IReadOnlyList<SubscriptionPlan> AvailableSubscriptionPlans { get; }

    public ICommand RefreshCommand { get; }

    public ICommand SelectSectionCommand { get; }

    public ICommand CreateOrganizationCommand { get; }

    public ICommand CreateBranchCommand { get; }

    public ICommand SaveBranchSettingsCommand { get; }

    public ICommand SwitchRoleCommand { get; }

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

    public OrganizationSection SelectedSection
    {
        get => _selectedSection;
        set => SetProperty(ref _selectedSection, value);
    }

    public DateTimeOffset? LastUpdated
    {
        get => _lastUpdated;
        private set => SetProperty(ref _lastUpdated, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public OrganizationDto? SelectedOrganization
    {
        get => _selectedOrganization;
        set
        {
            if (SetProperty(ref _selectedOrganization, value))
            {
                _ = LoadBranchesForSelectedOrganizationAsync();
            }
        }
    }

    public BranchDto? SelectedBranch
    {
        get => _selectedBranch;
        set
        {
            if (SetProperty(ref _selectedBranch, value))
            {
                _ = LoadSettingsForSelectedBranchAsync();
            }
        }
    }

    public BranchSettingsDto? CurrentBranchSettings
    {
        get => _currentBranchSettings;
        private set => SetProperty(ref _currentBranchSettings, value);
    }

    public string NewOrgName
    {
        get => _newOrgName;
        set => SetProperty(ref _newOrgName, value);
    }

    public string NewOrgLegalName
    {
        get => _newOrgLegalName;
        set => SetProperty(ref _newOrgLegalName, value);
    }

    public string NewOrgTaxInformation
    {
        get => _newOrgTaxInformation;
        set => SetProperty(ref _newOrgTaxInformation, value);
    }

    public SubscriptionPlan NewOrgSubscription
    {
        get => _newOrgSubscription;
        set => SetProperty(ref _newOrgSubscription, value);
    }

    public string NewBranchName
    {
        get => _newBranchName;
        set => SetProperty(ref _newBranchName, value);
    }

    public string NewBranchCode
    {
        get => _newBranchCode;
        set => SetProperty(ref _newBranchCode, value);
    }

    public string NewBranchAddress
    {
        get => _newBranchAddress;
        set => SetProperty(ref _newBranchAddress, value);
    }

    public string NewBranchPhone
    {
        get => _newBranchPhone;
        set => SetProperty(ref _newBranchPhone, value);
    }

    public string NewBranchEmail
    {
        get => _newBranchEmail;
        set => SetProperty(ref _newBranchEmail, value);
    }

    public string NewBranchManager
    {
        get => _newBranchManager;
        set => SetProperty(ref _newBranchManager, value);
    }

    public string NewBranchTimeZone
    {
        get => _newBranchTimeZone;
        set => SetProperty(ref _newBranchTimeZone, value);
    }

    public string NewBranchCurrency
    {
        get => _newBranchCurrency;
        set => SetProperty(ref _newBranchCurrency, value);
    }

    public string SettingsOpenTime
    {
        get => _settingsOpenTime;
        set => SetProperty(ref _settingsOpenTime, value);
    }

    public string SettingsCloseTime
    {
        get => _settingsCloseTime;
        set => SetProperty(ref _settingsCloseTime, value);
    }

    public bool SettingsMonday { get => _settingsMonday; set => SetProperty(ref _settingsMonday, value); }

    public bool SettingsTuesday { get => _settingsTuesday; set => SetProperty(ref _settingsTuesday, value); }

    public bool SettingsWednesday { get => _settingsWednesday; set => SetProperty(ref _settingsWednesday, value); }

    public bool SettingsThursday { get => _settingsThursday; set => SetProperty(ref _settingsThursday, value); }

    public bool SettingsFriday { get => _settingsFriday; set => SetProperty(ref _settingsFriday, value); }

    public bool SettingsSaturday { get => _settingsSaturday; set => SetProperty(ref _settingsSaturday, value); }

    public bool SettingsSunday { get => _settingsSunday; set => SetProperty(ref _settingsSunday, value); }

    public decimal SettingsVatPercentage
    {
        get => _settingsVatPercentage;
        set => SetProperty(ref _settingsVatPercentage, value);
    }

    public string SettingsReceiptHeader
    {
        get => _settingsReceiptHeader;
        set => SetProperty(ref _settingsReceiptHeader, value);
    }

    public string SettingsReceiptFooter
    {
        get => _settingsReceiptFooter;
        set => SetProperty(ref _settingsReceiptFooter, value);
    }

    public bool SettingsShowLogo
    {
        get => _settingsShowLogo;
        set => SetProperty(ref _settingsShowLogo, value);
    }

    public int SettingsMinNoticeHours
    {
        get => _settingsMinNoticeHours;
        set => SetProperty(ref _settingsMinNoticeHours, value);
    }

    public int SettingsMaxAdvanceBookingDays
    {
        get => _settingsMaxAdvanceBookingDays;
        set => SetProperty(ref _settingsMaxAdvanceBookingDays, value);
    }

    public bool SettingsAllowSameDayBooking
    {
        get => _settingsAllowSameDayBooking;
        set => SetProperty(ref _settingsAllowSameDayBooking, value);
    }

    public bool SettingsEmailEnabled
    {
        get => _settingsEmailEnabled;
        set => SetProperty(ref _settingsEmailEnabled, value);
    }

    public bool SettingsSmsEnabled
    {
        get => _settingsSmsEnabled;
        set => SetProperty(ref _settingsSmsEnabled, value);
    }

    public int SettingsReminderHoursBeforeAppointment
    {
        get => _settingsReminderHoursBeforeAppointment;
        set => SetProperty(ref _settingsReminderHoursBeforeAppointment, value);
    }

    public WorkspaceRole SelectedRoleToSwitchTo
    {
        get => _selectedRoleToSwitchTo;
        set => SetProperty(ref _selectedRoleToSwitchTo, value);
    }

    public string CurrentOrganizationName => _currentSessionService.CurrentOrganization?.Name ?? string.Empty;

    public string CurrentBranchName => _currentSessionService.CurrentBranch?.Name ?? string.Empty;

    public WorkspaceRole CurrentRole => _currentSessionService.CurrentRole;

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var organizations = await _queryService.GetOrganizationsAsync().ConfigureAwait(true);
            Organizations.Clear();
            foreach (var organization in organizations)
            {
                Organizations.Add(organization);
            }

            var toSelect = Organizations.FirstOrDefault(o => o.Id == SelectedOrganization?.Id) ?? Organizations.FirstOrDefault();
            SetProperty(ref _selectedOrganization, toSelect, nameof(SelectedOrganization));
            await LoadBranchesForSelectedOrganizationAsync().ConfigureAwait(true);

            LastUpdated = DateTimeOffset.Now;
            State = Organizations.Count == 0 ? DashboardState.Empty : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
        }
    }

    /// <summary>
    /// Loads every branch for <see cref="SelectedOrganization"/> - the
    /// concrete "Repository Filtering"/"no cross-branch data leakage"
    /// demonstration: <see cref="IOrganizationQueryService.GetBranchesAsync"/>
    /// only ever returns branches for the one organization ID passed in.
    /// Always re-runs (never short-circuited by value equality), so
    /// <see cref="RefreshCommand"/> genuinely reloads even when the
    /// selection itself hasn't changed.
    /// </summary>
    private async Task LoadBranchesForSelectedOrganizationAsync()
    {
        Branches.Clear();

        if (SelectedOrganization is null)
        {
            SetProperty(ref _selectedBranch, null, nameof(SelectedBranch));
            CurrentBranchSettings = null;
            return;
        }

        var branches = await _queryService.GetBranchesAsync(SelectedOrganization.Id).ConfigureAwait(true);
        foreach (var branch in branches)
        {
            Branches.Add(branch);
        }

        var branchToSelect = Branches.FirstOrDefault(b => b.Id == SelectedBranch?.Id) ?? Branches.FirstOrDefault();
        SetProperty(ref _selectedBranch, branchToSelect, nameof(SelectedBranch));
        await LoadSettingsForSelectedBranchAsync().ConfigureAwait(true);
    }

    private async Task LoadSettingsForSelectedBranchAsync()
    {
        if (SelectedBranch is null)
        {
            CurrentBranchSettings = null;
            return;
        }

        var settings = await _queryService.GetBranchSettingsAsync(SelectedBranch.Id).ConfigureAwait(true);
        CurrentBranchSettings = settings;

        if (settings is not null)
        {
            SettingsOpenTime = settings.BusinessHours.OpenTime.ToString("HH:mm", CultureInfo.InvariantCulture);
            SettingsCloseTime = settings.BusinessHours.CloseTime.ToString("HH:mm", CultureInfo.InvariantCulture);
            SettingsMonday = settings.WorkingDays.Contains(DayOfWeek.Monday);
            SettingsTuesday = settings.WorkingDays.Contains(DayOfWeek.Tuesday);
            SettingsWednesday = settings.WorkingDays.Contains(DayOfWeek.Wednesday);
            SettingsThursday = settings.WorkingDays.Contains(DayOfWeek.Thursday);
            SettingsFriday = settings.WorkingDays.Contains(DayOfWeek.Friday);
            SettingsSaturday = settings.WorkingDays.Contains(DayOfWeek.Saturday);
            SettingsSunday = settings.WorkingDays.Contains(DayOfWeek.Sunday);
            SettingsVatPercentage = settings.VatPercentage;
            SettingsReceiptHeader = settings.ReceiptSettings.HeaderText;
            SettingsReceiptFooter = settings.ReceiptSettings.FooterText;
            SettingsShowLogo = settings.ReceiptSettings.ShowLogo;
            SettingsMinNoticeHours = settings.AppointmentRules.MinNoticeHours;
            SettingsMaxAdvanceBookingDays = settings.AppointmentRules.MaxAdvanceBookingDays;
            SettingsAllowSameDayBooking = settings.AppointmentRules.AllowSameDayBooking;
            SettingsEmailEnabled = settings.NotificationSettings.EmailEnabled;
            SettingsSmsEnabled = settings.NotificationSettings.SmsEnabled;
            SettingsReminderHoursBeforeAppointment = settings.NotificationSettings.ReminderHoursBeforeAppointment;
        }
    }

    private async Task CreateOrganizationAsync()
    {
        await _commandService.CreateOrganizationAsync(NewOrgName, NewOrgLegalName, NewOrgTaxInformation, NewOrgSubscription).ConfigureAwait(true);
        NewOrgName = string.Empty;
        NewOrgLegalName = string.Empty;
        NewOrgTaxInformation = string.Empty;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task CreateBranchAsync()
    {
        if (SelectedOrganization is null)
        {
            return;
        }

        await _commandService.CreateBranchAsync(
            SelectedOrganization.Id, NewBranchName, NewBranchCode, NewBranchAddress, NewBranchPhone, NewBranchEmail, NewBranchManager, NewBranchTimeZone, NewBranchCurrency)
            .ConfigureAwait(true);

        NewBranchName = string.Empty;
        NewBranchCode = string.Empty;
        NewBranchAddress = string.Empty;
        NewBranchPhone = string.Empty;
        NewBranchEmail = string.Empty;
        NewBranchManager = string.Empty;
        NewBranchTimeZone = string.Empty;
        NewBranchCurrency = string.Empty;

        await LoadBranchesForSelectedOrganizationAsync().ConfigureAwait(true);
    }

    private async Task SaveBranchSettingsAsync()
    {
        if (SelectedBranch is null)
        {
            return;
        }

        var workingDays = new List<DayOfWeek>();
        if (SettingsMonday)
        {
            workingDays.Add(DayOfWeek.Monday);
        }

        if (SettingsTuesday)
        {
            workingDays.Add(DayOfWeek.Tuesday);
        }

        if (SettingsWednesday)
        {
            workingDays.Add(DayOfWeek.Wednesday);
        }

        if (SettingsThursday)
        {
            workingDays.Add(DayOfWeek.Thursday);
        }

        if (SettingsFriday)
        {
            workingDays.Add(DayOfWeek.Friday);
        }

        if (SettingsSaturday)
        {
            workingDays.Add(DayOfWeek.Saturday);
        }

        if (SettingsSunday)
        {
            workingDays.Add(DayOfWeek.Sunday);
        }

        var settings = new BranchSettingsDto(
            SelectedBranch.Id,
            new BusinessHoursDto(
                TimeOnly.Parse(SettingsOpenTime, CultureInfo.InvariantCulture),
                TimeOnly.Parse(SettingsCloseTime, CultureInfo.InvariantCulture)),
            workingDays,
            SettingsVatPercentage,
            new ReceiptSettingsDto(SettingsReceiptHeader, SettingsReceiptFooter, SettingsShowLogo),
            new AppointmentRulesDto(SettingsMinNoticeHours, SettingsMaxAdvanceBookingDays, SettingsAllowSameDayBooking),
            new NotificationSettingsDto(SettingsEmailEnabled, SettingsSmsEnabled, SettingsReminderHoursBeforeAppointment));

        CurrentBranchSettings = await _commandService.SetBranchSettingsAsync(settings).ConfigureAwait(true);
        StatusMessage = Rojan.Desktop.Presentation.Localization.Strings.Organizations_SettingsSaved;
    }

    private async Task SwitchRoleAsync()
    {
        await _currentSessionService.SwitchRoleAsync(SelectedRoleToSwitchTo).ConfigureAwait(true);
        OnPropertyChanged(nameof(CurrentRole));
        OnPropertyChanged(nameof(CurrentOrganizationName));
        OnPropertyChanged(nameof(CurrentBranchName));
    }
}
