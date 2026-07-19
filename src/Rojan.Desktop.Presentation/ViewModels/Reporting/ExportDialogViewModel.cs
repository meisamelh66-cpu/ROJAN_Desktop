using System.Windows.Input;
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
public sealed class ExportDialogViewModel : ViewModelBase
{
    private readonly IReportExportService _exportService;
    private readonly IDialogService _dialogService;
    private readonly ReportResultDto _result;

    private ExportFormat _selectedFormat = ExportFormat.Csv;
    private bool _isExporting;
    private string _statusMessage = string.Empty;

    public ExportDialogViewModel(ReportResultDto result, IReportExportService exportService, IDialogService dialogService)
    {
        _result = result;
        _exportService = exportService;
        _dialogService = dialogService;

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

    public ICommand ExportCommand { get; }

    public ICommand CloseCommand { get; }

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
        }
        finally
        {
            IsExporting = false;
        }
    }
}
