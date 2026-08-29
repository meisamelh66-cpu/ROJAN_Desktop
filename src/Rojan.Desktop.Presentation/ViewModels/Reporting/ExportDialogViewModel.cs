using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Reporting;
using Rojan.Desktop.Presentation.Dialogs;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Reporting;

/// <summary>
/// The Export Dialog - shown via <see cref="IDialogService.ShowDialog"/>
/// the same way <c>Accounting.PosCheckoutViewModel</c> is, resolved
/// through the same Views.xaml DataTemplate registry. Csv genuinely
/// writes a file and reports its path; Pdf/Excel/Print report the honest
/// "not yet implemented" stub message from
/// <see cref="IReportExportService"/> rather than pretending to succeed.
/// </summary>
public sealed partial class ExportDialogViewModel : ViewModelBase
{
    private readonly IReportExportService _exportService;
    private readonly IDialogService _dialogService;
    private readonly ReportResultDto _result;
    private readonly ILogger<ExportDialogViewModel> _logger;

    private ExportFormat _selectedFormat = ExportFormat.Csv;
    private bool _isExporting;
    private string _statusMessage = string.Empty;
    private string? _actionErrorMessage;
    private bool _hasActionError;

    public ExportDialogViewModel(ReportResultDto result, IReportExportService exportService, IDialogService dialogService, ILoggerFactory? loggerFactory = null)
    {
        _result = result;
        _exportService = exportService;
        _dialogService = dialogService;
        _logger = loggerFactory?.CreateLogger<ExportDialogViewModel>() ?? NullLogger<ExportDialogViewModel>.Instance;

        AvailableFormats = [ExportFormat.Csv, ExportFormat.Pdf, ExportFormat.Excel, ExportFormat.Print];
        ExportCommand = new AsyncRelayCommand(_ => ExportAsync(), _ => !IsExporting);
        CloseCommand = new RelayCommand(_ => _dialogService.CloseDialog());
    }

    public string ReportName => _result.ReportName;

    public IReadOnlyList<ExportFormat> AvailableFormats { get; }

    public ExportFormat SelectedFormat
    {
        get => _selectedFormat;
        set => SetProperty(ref _selectedFormat, value);
    }

    public bool IsExporting
    {
        get => _isExporting;
        private set => SetProperty(ref _isExporting, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Non-destructive inline error shown when an export fails unexpectedly
    /// (a file-system / permission failure, or an unknown-format throw) - the
    /// generic, never-raw-exception surface. Distinct from
    /// <see cref="StatusMessage"/>, which continues to carry the honest export
    /// result message for the handled paths (CSV success, "not yet implemented").
    /// Production Hardening missing-guard sweep (Export Dialog micro-phase).
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

    public ICommand ExportCommand { get; }

    public ICommand CloseCommand { get; }

    // Security: logs the operation name only - never the exception, its message,
    // the target file path, or any report content.
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Export dialog operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);

    private async Task ExportAsync()
    {
        IsExporting = true;
        StatusMessage = string.Empty;
        try
        {
            var result = await _exportService.ExportAsync(_result, SelectedFormat).ConfigureAwait(true);
            StatusMessage = result.Success && result.FilePath is not null
                ? $"{result.Message} ({result.FilePath})"
                : result.Message;
            ActionErrorMessage = null;
            HasActionError = false;
        }
#pragma warning disable CA1031 // Command boundary: an unexpected export failure (file-system / permission / unknown-format) must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Localization.Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(ExportAsync));
        }
        finally
        {
            IsExporting = false;
        }
    }
}
