using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Reporting;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Requires <see cref="Permission.ReportingView"/> - saving/removing run history is part of viewing reports, distinct from <see cref="Permission.ReportingExport"/>.</summary>
public sealed class ReportSnapshotCommandServicePermissionGate : IReportSnapshotCommandService
{
    private readonly IReportSnapshotCommandService _inner;
    private readonly IPermissionGate _permissionGate;

    public ReportSnapshotCommandServicePermissionGate(IReportSnapshotCommandService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<ReportSnapshotDto> RecordSnapshotAsync(ReportResultDto result, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.ReportingView);
        return _inner.RecordSnapshotAsync(result, cancellationToken);
    }

    public Task<ReportSnapshotDto> ToggleSavedAsync(string snapshotId, bool isSaved, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.ReportingView);
        return _inner.ToggleSavedAsync(snapshotId, isSaved, cancellationToken);
    }

    public Task DeleteSnapshotAsync(string snapshotId, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.ReportingView);
        return _inner.DeleteSnapshotAsync(snapshotId, cancellationToken);
    }
}
