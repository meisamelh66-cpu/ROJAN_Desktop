using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Reporting;
using Rojan.Desktop.Presentation.Dialogs;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Reporting;

/// <summary>
/// Drives the Reporting page: the report catalog (Catalog section), the
/// Report Viewer (pick a report, build filters via the Filter Panel, Run -
/// cancellable, never blocks the UI thread - see <see cref="RunReportCommand"/>/
/// <see cref="CancelCommand"/>), and Saved/Recent Reports (report-run
/// history, backed by <see cref="IReportSnapshotQueryService"/>). Export
/// opens <see cref="ExportDialogViewModel"/> through the existing dialog
/// framework, the same way <c>Accounting.AccountingPageViewModel</c> shows
/// its POS checkout dialog.
/// </summary>
public sealed partial class ReportingPageViewModel : ViewModelBase, IDisposable
{
    private readonly IReportCatalogQueryService _catalogQueryService;
    private readonly IReportExecutionQueryService _executionQueryService;
    private readonly IReportSnapshotQueryService _snapshotQueryService;
    private readonly IReportSnapshotCommandService _snapshotCommandService;
    private readonly IReportExportService _exportService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ReportingPageViewModel> _logger;

    private CancellationTokenSource? _runCancellation;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private string? _actionErrorMessage;
    private bool _hasActionError;
    private ReportingSection _selectedSection = ReportingSection.Catalog;
    private ReportDefinitionDto? _selectedReport;
    private ReportResultDto? _currentResult;
    private bool _isRunning;
    private string _statusMessage = string.Empty;
    private DateTime? _filterStartDate;
    private DateTime? _filterEndDate;
    private string _catalogSearchText = string.Empty;
    private IReadOnlyList<ReportDefinitionDto> _allReportDefinitions = [];

    public ReportingPageViewModel(
        IReportCatalogQueryService catalogQueryService,
        IReportExecutionQueryService executionQueryService,
        IReportSnapshotQueryService snapshotQueryService,
        IReportSnapshotCommandService snapshotCommandService,
        IReportExportService exportService,
        IDialogService dialogService,
        ILogger<ReportingPageViewModel>? logger = null)
    {
        _catalogQueryService = catalogQueryService;
        _executionQueryService = executionQueryService;
        _snapshotQueryService = snapshotQueryService;
        _snapshotCommandService = snapshotCommandService;
        _exportService = exportService;
        _dialogService = dialogService;
        _logger = logger ?? NullLogger<ReportingPageViewModel>.Instance;

        ReportDefinitions = new ObservableCollection<ReportDefinitionDto>();
        AdditionalFilters = new ObservableCollection<FilterEntryViewModel>();
        RecentSnapshots = new ObservableCollection<ReportSnapshotDto>();
        SavedSnapshots = new ObservableCollection<ReportSnapshotDto>();

        SelectSectionCommand = new RelayCommand(section => SelectedSection = (ReportingSection)section!);
        SelectReportCommand = new RelayCommand(report =>
        {
            SelectedReport = report as ReportDefinitionDto;
            SelectedSection = ReportingSection.Viewer;
        });
        AddFilterCommand = new RelayCommand(
            _ => AdditionalFilters.Add(new FilterEntryViewModel(SelectedReport!.SupportedFilters.FirstOrDefault(f => f != FilterType.DateRange))),
            _ => SelectedReport is not null && SelectedReport.SupportedFilters.Any(f => f != FilterType.DateRange));
        RemoveFilterCommand = new RelayCommand(entry => AdditionalFilters.Remove((FilterEntryViewModel)entry!));
        RunReportCommand = new AsyncRelayCommand(_ => RunReportAsync(), _ => SelectedReport is not null && !IsRunning);
        CancelCommand = new RelayCommand(_ => _runCancellation?.Cancel(), _ => IsRunning);
        OpenExportDialogCommand = new RelayCommand(_ => OpenExportDialog(), _ => CurrentResult is not null);
        ToggleSavedCommand = new AsyncRelayCommand(snapshot => ToggleSavedAsync((ReportSnapshotDto)snapshot!));
        DeleteSnapshotCommand = new AsyncRelayCommand(snapshot => DeleteSnapshotAsync((ReportSnapshotDto)snapshot!));
        RerunSnapshotCommand = new AsyncRelayCommand(snapshot => RerunSnapshotAsync((ReportSnapshotDto)snapshot!));
        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());

        _ = LoadAsync();
    }

    public ObservableCollection<ReportDefinitionDto> ReportDefinitions { get; }

    public ObservableCollection<FilterEntryViewModel> AdditionalFilters { get; }

    public ObservableCollection<ReportSnapshotDto> RecentSnapshots { get; }

    public ObservableCollection<ReportSnapshotDto> SavedSnapshots { get; }

    public ICommand SelectSectionCommand { get; }

    public ICommand SelectReportCommand { get; }

    public ICommand AddFilterCommand { get; }

    public ICommand RemoveFilterCommand { get; }

    public ICommand RunReportCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand OpenExportDialogCommand { get; }

    public ICommand ToggleSavedCommand { get; }

    public ICommand DeleteSnapshotCommand { get; }

    public ICommand RerunSnapshotCommand { get; }

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

    /// <summary>
    /// Non-destructive inline error shown when a snapshot pin/unpin or delete
    /// command fails - unlike <see cref="ErrorMessage"/>/<see cref="State"/> it
    /// never blanks the page, and unlike <see cref="StatusMessage"/> it does not
    /// clobber the last report-run status. Production Hardening missing-guard
    /// sweep (Reporting mini-wave), same shape as HR.HrPageViewModel.ActionErrorMessage.
    /// </summary>
    public string? ActionErrorMessage
    {
        get => _actionErrorMessage;
        private set => SetProperty(ref _actionErrorMessage, value);
    }

    public bool HasActionError
    {
        get => _hasActionError;
        private set => SetProperty(ref _hasActionError, value);
    }

    public ReportingSection SelectedSection
    {
        get => _selectedSection;
        set => SetProperty(ref _selectedSection, value);
    }

    public ReportDefinitionDto? SelectedReport
    {
        get => _selectedReport;
        set
        {
            if (SetProperty(ref _selectedReport, value))
            {
                CurrentResult = null;
                AdditionalFilters.Clear();
                FilterStartDate = null;
                FilterEndDate = null;
            }
        }
    }

    public ReportResultDto? CurrentResult
    {
        get => _currentResult;
        private set => SetProperty(ref _currentResult, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public DateTime? FilterStartDate
    {
        get => _filterStartDate;
        set => SetProperty(ref _filterStartDate, value);
    }

    public DateTime? FilterEndDate
    {
        get => _filterEndDate;
        set => SetProperty(ref _filterEndDate, value);
    }

    /// <summary>Filters the already-loaded catalog client-side (the full catalog is small and fully in memory, unlike Customers/Bookings' server-side search) - Requirement 33.2/33.13's global report search.</summary>
    public string CatalogSearchText
    {
        get => _catalogSearchText;
        set
        {
            if (SetProperty(ref _catalogSearchText, value))
            {
                ApplyCatalogFilter();
            }
        }
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            _allReportDefinitions = await _catalogQueryService.GetReportDefinitionsAsync().ConfigureAwait(true);
            ApplyCatalogFilter();

            await ReloadSnapshotsAsync().ConfigureAwait(true);

            State = _allReportDefinitions.Count == 0 ? DashboardState.Empty : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
            LogOperationFailed(nameof(LoadAsync));
        }
    }

    // Security: logs the operation name only - never the exception, its message,
    // report data/filters, or any backend response detail.
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Reporting page operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);

    private void ApplyCatalogFilter()
    {
        var matching = string.IsNullOrWhiteSpace(CatalogSearchText)
            ? _allReportDefinitions
            : _allReportDefinitions.Where(definition =>
                definition.Name.Contains(CatalogSearchText, StringComparison.OrdinalIgnoreCase)
                || definition.Description.Contains(CatalogSearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        ReportDefinitions.Clear();
        foreach (var definition in matching)
        {
            ReportDefinitions.Add(definition);
        }
    }

    private async Task ReloadSnapshotsAsync()
    {
        var recent = await _snapshotQueryService.GetRecentSnapshotsAsync().ConfigureAwait(true);
        RecentSnapshots.Clear();
        foreach (var snapshot in recent)
        {
            RecentSnapshots.Add(snapshot);
        }

        var saved = await _snapshotQueryService.GetSavedSnapshotsAsync().ConfigureAwait(true);
        SavedSnapshots.Clear();
        foreach (var snapshot in saved)
        {
            SavedSnapshots.Add(snapshot);
        }
    }

    private async Task RunReportAsync()
    {
        if (SelectedReport is null)
        {
            return;
        }

        _runCancellation?.Cancel();
        _runCancellation?.Dispose();
        _runCancellation = new CancellationTokenSource();
        var token = _runCancellation.Token;

        IsRunning = true;
        StatusMessage = string.Empty;
        try
        {
            var filters = BuildFilters();
            var result = await _executionQueryService.RunReportAsync(SelectedReport.Id, filters, token).ConfigureAwait(true);
            CurrentResult = result;
            await _snapshotCommandService.RecordSnapshotAsync(result, token).ConfigureAwait(true);
            await ReloadSnapshotsAsync().ConfigureAwait(true);
            StatusMessage = $"{result.Rows.Count} {Localization.Strings.Reporting_Rows}";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Localization.Strings.Reporting_RunCancelled;
        }
#pragma warning disable CA1031 // Report generation must surface any failure as a status message, not crash the page.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            StatusMessage = exception.Message;
            LogOperationFailed(nameof(RunReportAsync));
        }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task RerunSnapshotAsync(ReportSnapshotDto snapshot)
    {
        var definition = ReportDefinitions.FirstOrDefault(d => d.Id == snapshot.ReportDefinitionId);
        if (definition is null)
        {
            return;
        }

        SelectedReport = definition;
        SelectedSection = ReportingSection.Viewer;

        IsRunning = true;
        StatusMessage = string.Empty;
        try
        {
            CurrentResult = await _executionQueryService.RunReportAsync(definition.Id, snapshot.AppliedFilters).ConfigureAwait(true);
            StatusMessage = $"{CurrentResult.Rows.Count} {Localization.Strings.Reporting_Rows}";
        }
#pragma warning disable CA1031 // Report generation must surface any failure as a status message, not crash the page.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            StatusMessage = exception.Message;
            LogOperationFailed(nameof(RerunSnapshotAsync));
        }
        finally
        {
            IsRunning = false;
        }
    }

    private List<ReportFilterDto> BuildFilters()
    {
        var filters = new List<ReportFilterDto>();
        if (FilterStartDate is { } start && FilterEndDate is { } end)
        {
            filters.Add(new ReportFilterDto(
                FilterType.DateRange,
                $"{start:O}|{end:O}",
                $"{start:d} - {end:d}"));
        }

        filters.AddRange(AdditionalFilters.Where(entry => !string.IsNullOrWhiteSpace(entry.Value)).Select(entry => entry.ToDto()));
        return filters;
    }

    private void OpenExportDialog()
    {
        if (CurrentResult is null)
        {
            return;
        }

        _dialogService.ShowDialog(new ExportDialogViewModel(CurrentResult, _exportService, _dialogService));
    }

    private async Task ToggleSavedAsync(ReportSnapshotDto snapshot)
    {
        try
        {
            await _snapshotCommandService.ToggleSavedAsync(snapshot.Id, !snapshot.IsSaved).ConfigureAwait(true);
            await ReloadSnapshotsAsync().ConfigureAwait(true);
            ActionErrorMessage = null;
            HasActionError = false;
        }
#pragma warning disable CA1031 // Command boundary: a failed snapshot pin/unpin must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Localization.Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(ToggleSavedAsync));
        }
    }

    private async Task DeleteSnapshotAsync(ReportSnapshotDto snapshot)
    {
        try
        {
            await _snapshotCommandService.DeleteSnapshotAsync(snapshot.Id).ConfigureAwait(true);
            await ReloadSnapshotsAsync().ConfigureAwait(true);
            ActionErrorMessage = null;
            HasActionError = false;
        }
#pragma warning disable CA1031 // Command boundary: a failed snapshot delete must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Localization.Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(DeleteSnapshotAsync));
        }
    }

    /// <summary>Releases <see cref="_runCancellation"/> - this ViewModel is registered transient and, like every other page ViewModel, is replaced (not explicitly disposed) on navigation away; implementing <see cref="IDisposable"/> here satisfies CA1001 and gives a real cleanup path if a future navigation-aware disposal hook is added.</summary>
    public void Dispose() => _runCancellation?.Dispose();
}
