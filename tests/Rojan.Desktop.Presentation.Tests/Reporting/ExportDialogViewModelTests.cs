using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Reporting;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Dialogs;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Reporting;

namespace Rojan.Desktop.Presentation.Tests.Reporting;

/// <summary>
/// <see cref="ExportDialogViewModel"/> tests. Phase 8.86 (Missing-Guard Sweep
/// Export Dialog micro-phase) adds the <c>ExportAsync</c> failure guard and
/// brings the export flow into the operation-name-only logging pattern via an
/// injected <see cref="ILoggerFactory"/> (forwarded by
/// <c>ReportingPageViewModel.OpenExportDialog</c>).
/// </summary>
public sealed class ExportDialogViewModelTests
{
    private const string CustomerRow = "Amelia Hart";

    private static ReportResultDto MakeResult() => new(
        "revenue-report",
        "Revenue Report",
        DateTimeOffset.UnixEpoch,
        [new ReportColumnDto("customer", "Customer", ReportColumnDataType.Text)],
        [new ReportRowDto(new Dictionary<string, string> { ["customer"] = CustomerRow, ["total"] = "1,850,000" })],
        [],
        new Dictionary<string, string>());

    private static ExportDialogViewModel CreateSut(
        StubReportExportService? exportService = null,
        RecordingLoggerFactory? loggerFactory = null) =>
        new(MakeResult(), exportService ?? new StubReportExportService(), new StubDialogService(), loggerFactory);

    [Fact]
    public void ExportCommand_Success_ShowsResultMessageWithPath_AndTogglesIsExporting()
    {
        var export = new StubReportExportService { Result = new ExportResultDto(true, "Exported 1 rows to CSV.", @"C:\temp\RojanDesktopExports\Revenue_Report.csv") };
        var sut = CreateSut(export);

        sut.ExportCommand.Execute(null);

        Assert.Equal(ExportFormat.Csv, export.LastFormat);
        Assert.Contains("Exported 1 rows to CSV.", sut.StatusMessage, StringComparison.Ordinal);
        Assert.Contains(@"C:\temp\RojanDesktopExports\Revenue_Report.csv", sut.StatusMessage, StringComparison.Ordinal);
        Assert.False(sut.IsExporting);
        Assert.False(sut.HasActionError);
        Assert.Null(sut.ActionErrorMessage);
    }

    [Fact]
    public void ExportCommand_NotYetImplementedFormat_ShowsHonestMessage_NoActionError()
    {
        var export = new StubReportExportService { Result = new ExportResultDto(false, "PDF export is not yet implemented - Phase 20 ships the export architecture only.", null) };
        var sut = CreateSut(export);
        sut.SelectedFormat = ExportFormat.Pdf;

        sut.ExportCommand.Execute(null);

        Assert.Equal("PDF export is not yet implemented - Phase 20 ships the export architecture only.", sut.StatusMessage);
        Assert.False(sut.HasActionError);
        Assert.Null(sut.ActionErrorMessage);
        Assert.False(sut.IsExporting);
    }

    [Fact]
    public void ExportCommand_Failure_DoesNotThrow_SetsActionErrorAndResetsIsExporting()
    {
        var export = new StubReportExportService { ExportException = new UnauthorizedAccessException($@"Access to the path 'C:\temp\RojanDesktopExports\Revenue_Report.csv' is denied. {CustomerRow}") };
        var sut = CreateSut(export);

        var exception = Record.Exception(() => sut.ExportCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.False(sut.IsExporting);                       // finally still ran
        Assert.Equal(string.Empty, sut.StatusMessage);       // guard does not write the error into StatusMessage
        Assert.Equal(ExportFormat.Csv, export.LastFormat);   // the export was attempted
    }

    [Fact]
    public void ExportCommand_Failure_LogsOperationNameOnly_NoPathOrReportContentLeak()
    {
        const string secretPath = @"C:\temp\RojanDesktopExports\Revenue_Report_20260828.csv";
        var export = new StubReportExportService { ExportException = new UnauthorizedAccessException($"Access to the path '{secretPath}' is denied. customer={CustomerRow} total=1,850,000") };
        var loggerFactory = new RecordingLoggerFactory();
        var sut = CreateSut(export, loggerFactory);

        sut.ExportCommand.Execute(null);

        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains(nameof(ExportDialogViewModel), entry.Category, StringComparison.Ordinal);
        Assert.Contains("Operation=ExportAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(secretPath, entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(CustomerRow, entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(secretPath, sut.ActionErrorMessage ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(CustomerRow, sut.ActionErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportCommand_Failure_WithoutLoggerFactory_UsesNullLogger_NeverThrows()
    {
        var export = new StubReportExportService { ExportException = new InvalidOperationException("boom") };
        var sut = CreateSut(export);

        var exception = Record.Exception(() => sut.ExportCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.False(sut.IsExporting);
    }

    [Fact]
    public void ExportCommand_SuccessAfterFailure_ClearsActionError()
    {
        var export = new StubReportExportService { ExportException = new InvalidOperationException("boom") };
        var sut = CreateSut(export);
        sut.ExportCommand.Execute(null);
        Assert.True(sut.HasActionError);

        export.ExportException = null;
        sut.ExportCommand.Execute(null);

        Assert.False(sut.HasActionError);
        Assert.Null(sut.ActionErrorMessage);
        Assert.Contains("Exported.", sut.StatusMessage, StringComparison.Ordinal);
    }
}
