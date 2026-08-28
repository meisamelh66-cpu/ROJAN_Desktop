using Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Presentation.Tests.Reporting;

internal sealed class StubReportCatalogQueryService(IReadOnlyList<ReportDefinitionDto> definitions) : IReportCatalogQueryService
{
    public Task<IReadOnlyList<ReportDefinitionDto>> GetReportDefinitionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(definitions);

    public Task<ReportDefinitionDto?> GetReportDefinitionByIdAsync(string reportDefinitionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(definitions.FirstOrDefault(d => d.Id == reportDefinitionId));
}

internal sealed class StubReportExecutionQueryService : IReportExecutionQueryService
{
    public int RunCount { get; private set; }

    public string? LastReportDefinitionId { get; private set; }

    public IReadOnlyList<ReportFilterDto>? LastFilters { get; private set; }

    public Func<string, IReadOnlyList<ReportFilterDto>, ReportResultDto>? ResultFactory { get; set; }

    public Task<ReportResultDto> RunReportAsync(string reportDefinitionId, IReadOnlyList<ReportFilterDto> filters, CancellationToken cancellationToken = default)
    {
        RunCount++;
        LastReportDefinitionId = reportDefinitionId;
        LastFilters = filters;

        var result = ResultFactory?.Invoke(reportDefinitionId, filters) ?? new ReportResultDto(
            reportDefinitionId,
            "Stub Report",
            DateTimeOffset.Now,
            [],
            [new ReportRowDto(new Dictionary<string, string>())],
            filters,
            new Dictionary<string, string>());

        return Task.FromResult(result);
    }
}

internal sealed class StubReportSnapshotQueryService : IReportSnapshotQueryService
{
    public List<ReportSnapshotDto> Recent { get; } = [];

    public List<ReportSnapshotDto> Saved { get; } = [];

    /// <summary>Production Hardening (missing-guard sweep, Reporting mini-wave): when set, GetRecentSnapshotsAsync throws this - lets a test exercise the ViewModel's guard when the follow-on ReloadSnapshotsAsync fails after a successful toggle/delete.</summary>
    public Exception? GetRecentSnapshotsException { get; set; }

    public Task<IReadOnlyList<ReportSnapshotDto>> GetRecentSnapshotsAsync(CancellationToken cancellationToken = default) =>
        GetRecentSnapshotsException is not null
            ? Task.FromException<IReadOnlyList<ReportSnapshotDto>>(GetRecentSnapshotsException)
            : Task.FromResult<IReadOnlyList<ReportSnapshotDto>>(Recent);

    public Task<IReadOnlyList<ReportSnapshotDto>> GetSavedSnapshotsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ReportSnapshotDto>>(Saved);
}

internal sealed class StubReportSnapshotCommandService : IReportSnapshotCommandService
{
    public int RecordCount { get; private set; }

    public string? LastToggledId { get; private set; }

    public bool? LastToggledValue { get; private set; }

    public string? LastDeletedId { get; private set; }

    /// <summary>Production Hardening (missing-guard sweep, Reporting mini-wave): when set, the matching command throws this instead of succeeding - lets a test exercise the ViewModel's new try/catch without a real backend failure. Same seam pattern as Customers.StubCustomerCommandService.CreateCustomerException. The call is still recorded before the throw.</summary>
    public Exception? ToggleSavedException { get; set; }

    public Exception? DeleteSnapshotException { get; set; }

    public Task<ReportSnapshotDto> RecordSnapshotAsync(ReportResultDto result, CancellationToken cancellationToken = default)
    {
        RecordCount++;
        return Task.FromResult(new ReportSnapshotDto($"snapshot-{RecordCount}", result.ReportDefinitionId, result.ReportName, result.GeneratedAt, result.AppliedFilters, result.Rows.Count, false));
    }

    public Task<ReportSnapshotDto> ToggleSavedAsync(string snapshotId, bool isSaved, CancellationToken cancellationToken = default)
    {
        LastToggledId = snapshotId;
        LastToggledValue = isSaved;
        return ToggleSavedException is not null
            ? Task.FromException<ReportSnapshotDto>(ToggleSavedException)
            : Task.FromResult(new ReportSnapshotDto(snapshotId, "revenue-report", "Revenue Report", DateTimeOffset.Now, [], 1, isSaved));
    }

    public Task DeleteSnapshotAsync(string snapshotId, CancellationToken cancellationToken = default)
    {
        LastDeletedId = snapshotId;
        return DeleteSnapshotException is not null ? Task.FromException(DeleteSnapshotException) : Task.CompletedTask;
    }
}

internal sealed class StubReportExportService : IReportExportService
{
    public ExportFormat? LastFormat { get; private set; }

    public Task<ExportResultDto> ExportAsync(ReportResultDto result, ExportFormat format, CancellationToken cancellationToken = default)
    {
        LastFormat = format;
        return Task.FromResult(new ExportResultDto(true, "Exported.", @"C:\temp\report.csv"));
    }
}

internal sealed class StubKpiEngineQueryService(IReadOnlyList<KpiValueDto> kpis) : IKpiEngineQueryService
{
    public Task<IReadOnlyList<KpiValueDto>> GetKpisAsync(AnalyticsPeriod period, CancellationToken cancellationToken = default) =>
        Task.FromResult(kpis);
}

internal sealed class StubAnalyticsQueryService(AnalyticsSummaryDto summary, IReadOnlyList<ChartDefinitionDto> charts) : IAnalyticsQueryService
{
    public Task<AnalyticsSummaryDto> GetAnalyticsSummaryAsync(AnalyticsPeriod period, CancellationToken cancellationToken = default) =>
        Task.FromResult(summary);

    public Task<IReadOnlyList<ChartDefinitionDto>> GetDashboardChartsAsync(AnalyticsPeriod period, CancellationToken cancellationToken = default) =>
        Task.FromResult(charts);
}
