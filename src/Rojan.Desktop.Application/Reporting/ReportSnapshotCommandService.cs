using DomainReporting = Rojan.Desktop.Domain.Reporting;

namespace Rojan.Desktop.Application.Reporting;

public sealed class ReportSnapshotCommandService : IReportSnapshotCommandService
{
    private readonly DomainReporting.IReportingRepository _repository;

    public ReportSnapshotCommandService(DomainReporting.IReportingRepository repository)
    {
        _repository = repository;
    }

    public async Task<ReportSnapshotDto> RecordSnapshotAsync(ReportResultDto result, CancellationToken cancellationToken = default)
    {
        var snapshot = new DomainReporting.ReportSnapshot(
            $"snapshot-{Guid.NewGuid():N}",
            result.ReportDefinitionId,
            result.ReportName,
            result.GeneratedAt,
            result.AppliedFilters.Select(ReportingMapper.MapFilter).ToList(),
            result.Rows.Count,
            false);

        var created = await _repository.CreateSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false);
        return ReportingMapper.MapSnapshot(created);
    }

    public async Task<ReportSnapshotDto> ToggleSavedAsync(string snapshotId, bool isSaved, CancellationToken cancellationToken = default)
    {
        var updated = await _repository.UpdateSnapshotSavedStateAsync(snapshotId, isSaved, cancellationToken).ConfigureAwait(false);
        return ReportingMapper.MapSnapshot(updated);
    }

    public Task DeleteSnapshotAsync(string snapshotId, CancellationToken cancellationToken = default) =>
        _repository.DeleteSnapshotAsync(snapshotId, cancellationToken);
}
