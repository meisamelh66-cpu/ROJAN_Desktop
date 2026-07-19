using DomainReporting = Rojan.Desktop.Domain.Reporting;

namespace Rojan.Desktop.Application.Reporting;

public sealed class ReportSnapshotQueryService : IReportSnapshotQueryService
{
    private readonly DomainReporting.IReportingRepository _repository;

    public ReportSnapshotQueryService(DomainReporting.IReportingRepository repository)
    {
        _repository = repository;
    }

    /// <summary>Every run, newest first, regardless of <see cref="ReportSnapshotDto.IsSaved"/> - "Saved Reports" is a pinned subset of this same history, not a separate history.</summary>
    public async Task<IReadOnlyList<ReportSnapshotDto>> GetRecentSnapshotsAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = await _repository.GetSnapshotsAsync(cancellationToken).ConfigureAwait(false);
        return snapshots.Take(10).Select(ReportingMapper.MapSnapshot).ToList();
    }

    public async Task<IReadOnlyList<ReportSnapshotDto>> GetSavedSnapshotsAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = await _repository.GetSnapshotsAsync(cancellationToken).ConfigureAwait(false);
        return snapshots.Where(snapshot => snapshot.IsSaved).Select(ReportingMapper.MapSnapshot).ToList();
    }
}
