namespace Rojan.Desktop.Application.Reporting;

/// <summary>
/// The Export architecture's single entry point for the Export Dialog.
/// Phase 20 ships this end-to-end for <see cref="ExportFormat.Csv"/> (a
/// genuine, working implementation) - Pdf/Excel/Print are honest stubs
/// (<see cref="ExportResultDto.Success"/> false with a clear message)
/// rather than a silent no-op or an unhandled exception, matching the
/// "Do NOT connect to servers yet" precedent Phase 19A's Language Store
/// Foundation set for architecture-only surfaces.
/// </summary>
public interface IReportExportService
{
    public Task<ExportResultDto> ExportAsync(ReportResultDto result, ExportFormat format, CancellationToken cancellationToken = default);
}
