using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Reporting;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Requires <see cref="Permission.ReportingExport"/>.</summary>
public sealed class ReportExportServicePermissionGate : IReportExportService
{
    private readonly IReportExportService _inner;
    private readonly IPermissionGate _permissionGate;

    public ReportExportServicePermissionGate(IReportExportService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<ExportResultDto> ExportAsync(ReportResultDto result, ExportFormat format, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.ReportingExport);
        return _inner.ExportAsync(result, format, cancellationToken);
    }
}
