namespace Rojan.Desktop.Domain.Reporting;

/// <summary>
/// Repository abstraction for the Reporting vertical slice. Unlike every
/// other module's repository, this one does NOT own the business data its
/// reports summarize (revenue, appointments, payroll, ...) - that lives in
/// each source module's own repository and reaches Reporting only through
/// their already-published Application-layer query services (see
/// <c>Application.Reporting.ReportExecutionQueryService</c>). This
/// repository owns exactly two things: the static report catalog
/// (<see cref="ReportDefinition"/>) and report-run history
/// (<see cref="ReportSnapshot"/>) - the only state genuinely local to
/// Reporting. "Dumb" like every other repository in this app: no
/// aggregation math here, that is Application's job.
/// </summary>
public interface IReportingRepository
{
    public Task<IReadOnlyList<ReportDefinition>> GetReportDefinitionsAsync(CancellationToken cancellationToken = default);

    public Task<ReportDefinition?> GetReportDefinitionByIdAsync(string reportDefinitionId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ReportSnapshot>> GetSnapshotsAsync(CancellationToken cancellationToken = default);

    public Task<ReportSnapshot> CreateSnapshotAsync(ReportSnapshot snapshot, CancellationToken cancellationToken = default);

    public Task<ReportSnapshot> UpdateSnapshotSavedStateAsync(string snapshotId, bool isSaved, CancellationToken cancellationToken = default);

    public Task DeleteSnapshotAsync(string snapshotId, CancellationToken cancellationToken = default);
}
