namespace Rojan.Desktop.Application.Reporting;

/// <summary>Backs the Report Viewer's "Recent Reports"/"Saved Reports" lists.</summary>
public interface IReportSnapshotQueryService
{
    public Task<IReadOnlyList<ReportSnapshotDto>> GetRecentSnapshotsAsync(CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ReportSnapshotDto>> GetSavedSnapshotsAsync(CancellationToken cancellationToken = default);
}
