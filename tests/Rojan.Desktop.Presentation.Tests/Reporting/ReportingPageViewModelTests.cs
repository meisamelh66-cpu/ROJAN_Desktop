using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Reporting;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Dialogs;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Reporting;

namespace Rojan.Desktop.Presentation.Tests.Reporting;

public sealed class ReportingPageViewModelTests
{
    private static ReportDefinitionDto MakeDefinition(string id = "revenue-report", string name = "Revenue Report") => new(
        id, name, "desc", ReportType.RevenueReport, ReportCategory.Financial, true,
        [new ReportColumnDto("date", "Date", ReportColumnDataType.Date)],
        [FilterType.DateRange, FilterType.Customer]);

    private static ReportSnapshotDto MakeSnapshot(string id = "snapshot-1", bool isSaved = false) =>
        new(id, "revenue-report", "Revenue Report", DateTimeOffset.Now, [], 3, isSaved);

    private static (ReportingPageViewModel Sut, StubReportExecutionQueryService Execution, StubReportSnapshotQueryService SnapshotQuery, StubReportSnapshotCommandService SnapshotCommand, StubDialogService Dialog) CreateSut(
        IReadOnlyList<ReportDefinitionDto>? definitions = null,
        RecordingLogger<ReportingPageViewModel>? logger = null,
        StubReportSnapshotQueryService? snapshotQuery = null,
        StubReportSnapshotCommandService? snapshotCommand = null)
    {
        var catalog = new StubReportCatalogQueryService(definitions ?? [MakeDefinition()]);
        var execution = new StubReportExecutionQueryService();
        var snapshotQueryService = snapshotQuery ?? new StubReportSnapshotQueryService();
        var snapshotCommandService = snapshotCommand ?? new StubReportSnapshotCommandService();
        var export = new StubReportExportService();
        var dialog = new StubDialogService();

        var sut = new ReportingPageViewModel(catalog, execution, snapshotQueryService, snapshotCommandService, export, dialog, logger);
        return (sut, execution, snapshotQueryService, snapshotCommandService, dialog);
    }

    [Fact]
    public void Constructor_LoadsReportDefinitionsIntoCollection()
    {
        var (sut, _, _, _, _) = CreateSut();

        Assert.Single(sut.ReportDefinitions);
        Assert.Equal("revenue-report", sut.ReportDefinitions[0].Id);
    }

    [Fact]
    public void SelectReportCommand_SetsSelectedReportAndSwitchesToViewerSection()
    {
        var (sut, _, _, _, _) = CreateSut();
        var definition = sut.ReportDefinitions[0];

        sut.SelectReportCommand.Execute(definition);

        Assert.Equal(definition, sut.SelectedReport);
        Assert.Equal(ReportingSection.Viewer, sut.SelectedSection);
    }

    [Fact]
    public void SelectingADifferentReport_ClearsCurrentResultAndFilters()
    {
        var (sut, _, _, _, _) = CreateSut();
        sut.SelectReportCommand.Execute(sut.ReportDefinitions[0]);
        sut.RunReportCommand.Execute(null);
        Assert.NotNull(sut.CurrentResult);

        sut.SelectedReport = null;

        Assert.Null(sut.CurrentResult);
    }

    [Fact]
    public void RunReportCommand_CanExecute_FalseWhenNoReportSelected()
    {
        var (sut, _, _, _, _) = CreateSut();

        Assert.False(sut.RunReportCommand.CanExecute(null));
    }

    [Fact]
    public void RunReportCommand_RunsSelectedReportAndPopulatesResult()
    {
        var (sut, execution, _, snapshotCommand, _) = CreateSut();
        sut.SelectReportCommand.Execute(sut.ReportDefinitions[0]);

        sut.RunReportCommand.Execute(null);

        Assert.Equal(1, execution.RunCount);
        Assert.Equal("revenue-report", execution.LastReportDefinitionId);
        Assert.NotNull(sut.CurrentResult);
        Assert.Equal(1, snapshotCommand.RecordCount);
        Assert.Contains("rows", sut.StatusMessage, StringComparison.Ordinal);
    }

    // Phase 8.19 Logging Wave 2A: LoadAsync / RunReportAsync / RerunSnapshotAsync now log at Error
    // before their existing handling - user-visible behaviour unchanged.

    [Fact]
    public void RunReportCommand_ExecutionThrows_LogsError()
    {
        var logger = new RecordingLogger<ReportingPageViewModel>();
        var (sut, execution, _, _, _) = CreateSut(logger: logger);
        execution.ResultFactory = (_, _) => throw new InvalidOperationException("boom");
        sut.SelectReportCommand.Execute(sut.ReportDefinitions[0]);

        sut.RunReportCommand.Execute(null);

        Assert.Equal("boom", sut.StatusMessage);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("RunReportAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_RunReportFailureNeverThrows()
    {
        var (sut, execution, _, _, _) = CreateSut();
        execution.ResultFactory = (_, _) => throw new InvalidOperationException("boom");
        sut.SelectReportCommand.Execute(sut.ReportDefinitions[0]);

        var exception = Record.Exception(() => sut.RunReportCommand.Execute(null));

        Assert.Null(exception);
        Assert.Equal("boom", sut.StatusMessage);
    }

    [Fact]
    public void RunReportCommand_WithDateRangeSet_IncludesDateRangeFilter()
    {
        var (sut, execution, _, _, _) = CreateSut();
        sut.SelectReportCommand.Execute(sut.ReportDefinitions[0]);
        sut.FilterStartDate = new DateTime(2026, 1, 1);
        sut.FilterEndDate = new DateTime(2026, 1, 31);

        sut.RunReportCommand.Execute(null);

        Assert.NotNull(execution.LastFilters);
        Assert.Contains(execution.LastFilters, f => f.FilterType == FilterType.DateRange);
    }

    [Fact]
    public void AddFilterCommand_AddsEntryForFirstSupportedNonDateRangeFilter()
    {
        var (sut, _, _, _, _) = CreateSut();
        sut.SelectReportCommand.Execute(sut.ReportDefinitions[0]);

        sut.AddFilterCommand.Execute(null);

        Assert.Single(sut.AdditionalFilters);
        Assert.Equal(FilterType.Customer, sut.AdditionalFilters[0].FilterType);
    }

    [Fact]
    public void RemoveFilterCommand_RemovesTheSpecifiedEntry()
    {
        var (sut, _, _, _, _) = CreateSut();
        sut.SelectReportCommand.Execute(sut.ReportDefinitions[0]);
        sut.AddFilterCommand.Execute(null);
        var entry = sut.AdditionalFilters[0];

        sut.RemoveFilterCommand.Execute(entry);

        Assert.Empty(sut.AdditionalFilters);
    }

    [Fact]
    public void OpenExportDialogCommand_CanExecute_FalseWithNoResult()
    {
        var (sut, _, _, _, _) = CreateSut();

        Assert.False(sut.OpenExportDialogCommand.CanExecute(null));
    }

    [Fact]
    public void OpenExportDialogCommand_AfterRunningReport_ShowsExportDialog()
    {
        var (sut, _, _, _, dialog) = CreateSut();
        sut.SelectReportCommand.Execute(sut.ReportDefinitions[0]);
        sut.RunReportCommand.Execute(null);

        sut.OpenExportDialogCommand.Execute(null);

        Assert.Single(dialog.ShownDialogs);
        Assert.IsType<ExportDialogViewModel>(dialog.ShownDialogs[0]);
    }

    [Fact]
    public void ToggleSavedCommand_CallsSnapshotCommandServiceWithInvertedState()
    {
        var (sut, _, _, snapshotCommand, _) = CreateSut();
        var snapshot = new ReportSnapshotDto("snapshot-1", "revenue-report", "Revenue Report", DateTimeOffset.Now, [], 3, false);

        sut.ToggleSavedCommand.Execute(snapshot);

        Assert.Equal("snapshot-1", snapshotCommand.LastToggledId);
        Assert.True(snapshotCommand.LastToggledValue);
    }

    [Fact]
    public void DeleteSnapshotCommand_CallsSnapshotCommandServiceDelete()
    {
        var (sut, _, _, snapshotCommand, _) = CreateSut();
        var snapshot = new ReportSnapshotDto("snapshot-1", "revenue-report", "Revenue Report", DateTimeOffset.Now, [], 3, false);

        sut.DeleteSnapshotCommand.Execute(snapshot);

        Assert.Equal("snapshot-1", snapshotCommand.LastDeletedId);
    }

    [Fact]
    public void RerunSnapshotCommand_SelectsMatchingDefinitionAndRunsWithStoredFilters()
    {
        var (sut, execution, _, _, _) = CreateSut();
        var storedFilters = new List<ReportFilterDto> { new(FilterType.Customer, "customer-1", "label") };
        var snapshot = new ReportSnapshotDto("snapshot-1", "revenue-report", "Revenue Report", DateTimeOffset.Now, storedFilters, 3, false);

        sut.RerunSnapshotCommand.Execute(snapshot);

        Assert.Equal("revenue-report", sut.SelectedReport?.Id);
        Assert.Equal(ReportingSection.Viewer, sut.SelectedSection);
        Assert.Equal(1, execution.RunCount);
        Assert.Equal(storedFilters, execution.LastFilters);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var (sut, _, _, _, _) = CreateSut();

        var exception = Record.Exception(sut.Dispose);

        Assert.Null(exception);
    }

    // ---------------------------------------------------------------------
    // Production Hardening - Missing-Guard Sweep Reporting mini-wave.
    // ToggleSavedAsync / DeleteSnapshotAsync now surface a backend failure via
    // the non-destructive ActionErrorMessage/HasActionError pair instead of the
    // global dialog. Failures never expose revenue / customer / report data,
    // and log operation-name-only. Report generation / cancellation / export
    // are untouched.
    // ---------------------------------------------------------------------

    private const string ReportBackendSecret = "backend 500: snapshot revenue-2026-Q1 total=1,850,000 customer=Amelia Hart";

    [Fact]
    public void ToggleSavedCommand_Failure_DoesNotThrow_SetsActionErrorAndDoesNotClobberStatus()
    {
        var command = new StubReportSnapshotCommandService { ToggleSavedException = new InvalidOperationException(ReportBackendSecret) };
        var (sut, _, _, _, _) = CreateSut(snapshotCommand: command);
        sut.SelectReportCommand.Execute(sut.ReportDefinitions[0]);
        sut.RunReportCommand.Execute(null);
        var statusAfterRun = sut.StatusMessage;

        var exception = Record.Exception(() => sut.ToggleSavedCommand.Execute(MakeSnapshot()));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.NotEqual(DashboardState.Error, sut.State);
        Assert.Equal(statusAfterRun, sut.StatusMessage);
        Assert.Equal("snapshot-1", command.LastToggledId);
    }

    [Fact]
    public void DeleteSnapshotCommand_Failure_DoesNotThrow_SetsActionErrorAndPreservesSnapshotList()
    {
        var query = new StubReportSnapshotQueryService();
        query.Saved.Add(MakeSnapshot("snapshot-1", isSaved: true));
        query.Recent.Add(MakeSnapshot("snapshot-1", isSaved: true));
        var command = new StubReportSnapshotCommandService { DeleteSnapshotException = new InvalidOperationException(ReportBackendSecret) };
        var (sut, _, _, _, _) = CreateSut(snapshotQuery: query, snapshotCommand: command);
        Assert.Single(sut.SavedSnapshots);

        var exception = Record.Exception(() => sut.DeleteSnapshotCommand.Execute(MakeSnapshot("snapshot-1")));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.Single(sut.SavedSnapshots);
        Assert.Single(sut.RecentSnapshots);
        Assert.Equal("snapshot-1", command.LastDeletedId);
    }

    [Fact]
    public void ToggleSavedCommand_ReloadFailsAfterSuccessfulToggle_DoesNotThrow_SetsActionError()
    {
        var query = new StubReportSnapshotQueryService();
        var (sut, _, snapshotQuery, _, _) = CreateSut(snapshotQuery: query);
        snapshotQuery.GetRecentSnapshotsException = new InvalidOperationException(ReportBackendSecret);

        var exception = Record.Exception(() => sut.ToggleSavedCommand.Execute(MakeSnapshot()));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
    }

    [Fact]
    public void ToggleSavedCommand_SuccessAfterFailure_ClearsActionError()
    {
        var command = new StubReportSnapshotCommandService { ToggleSavedException = new InvalidOperationException("boom") };
        var (sut, _, _, _, _) = CreateSut(snapshotCommand: command);
        sut.ToggleSavedCommand.Execute(MakeSnapshot());
        Assert.True(sut.HasActionError);

        command.ToggleSavedException = null;
        sut.ToggleSavedCommand.Execute(MakeSnapshot());

        Assert.False(sut.HasActionError);
        Assert.Null(sut.ActionErrorMessage);
    }

    [Fact]
    public void DeleteSnapshotCommand_Failure_LogsOperationNameOnly_NoRevenueOrCustomerLeak()
    {
        var command = new StubReportSnapshotCommandService { DeleteSnapshotException = new InvalidOperationException(ReportBackendSecret) };
        var logger = new RecordingLogger<ReportingPageViewModel>();
        var (sut, _, _, _, _) = CreateSut(logger: logger, snapshotCommand: command);

        sut.DeleteSnapshotCommand.Execute(MakeSnapshot());

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("Operation=DeleteSnapshotAsync", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains(ReportBackendSecret, StringComparison.Ordinal));
        Assert.DoesNotContain(ReportBackendSecret, sut.ActionErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void ToggleSavedCommand_Failure_LogsOperationNameOnly()
    {
        var command = new StubReportSnapshotCommandService { ToggleSavedException = new InvalidOperationException(ReportBackendSecret) };
        var logger = new RecordingLogger<ReportingPageViewModel>();
        var (sut, _, _, _, _) = CreateSut(logger: logger, snapshotCommand: command);

        sut.ToggleSavedCommand.Execute(MakeSnapshot());

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("Operation=ToggleSavedAsync", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains(ReportBackendSecret, StringComparison.Ordinal));
    }
}
