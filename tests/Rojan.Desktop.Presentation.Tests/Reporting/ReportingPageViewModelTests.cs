using Rojan.Desktop.Application.Reporting;
using Rojan.Desktop.Presentation.Tests.Dialogs;
using Rojan.Desktop.Presentation.ViewModels.Reporting;

namespace Rojan.Desktop.Presentation.Tests.Reporting;

public sealed class ReportingPageViewModelTests
{
    private static ReportDefinitionDto MakeDefinition(string id = "revenue-report", string name = "Revenue Report") => new(
        id, name, "desc", ReportType.RevenueReport, ReportCategory.Financial, true,
        [new ReportColumnDto("date", "Date", ReportColumnDataType.Date)],
        [FilterType.DateRange, FilterType.Customer]);

    private static (ReportingPageViewModel Sut, StubReportExecutionQueryService Execution, StubReportSnapshotQueryService SnapshotQuery, StubReportSnapshotCommandService SnapshotCommand, StubDialogService Dialog) CreateSut(
        IReadOnlyList<ReportDefinitionDto>? definitions = null)
    {
        var catalog = new StubReportCatalogQueryService(definitions ?? [MakeDefinition()]);
        var execution = new StubReportExecutionQueryService();
        var snapshotQuery = new StubReportSnapshotQueryService();
        var snapshotCommand = new StubReportSnapshotCommandService();
        var export = new StubReportExportService();
        var dialog = new StubDialogService();

        var sut = new ReportingPageViewModel(catalog, execution, snapshotQuery, snapshotCommand, export, dialog);
        return (sut, execution, snapshotQuery, snapshotCommand, dialog);
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
        Assert.Contains("Generated", sut.StatusMessage, StringComparison.Ordinal);
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
}
