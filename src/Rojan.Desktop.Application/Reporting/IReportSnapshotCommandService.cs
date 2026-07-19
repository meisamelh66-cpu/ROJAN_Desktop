namespace Rojan.Desktop.Application.Reporting;

/// <summary>Writes report-run history - recording a run and toggling/removing a "Saved Reports" pin.</summary>
public interface IReportSnapshotCommandService
{
    public Task<ReportSnapshotDto> RecordSnapshotAsync(ReportResultDto result, CancellationToken cancellationToken = default);

    public Task<ReportSnapshotDto> ToggleSavedAsync(string snapshotId, bool isSaved, CancellationToken cancellationToken = default);

    public Task DeleteSnapshotAsync(string snapshotId, CancellationToken cancellationToken = default);
}
