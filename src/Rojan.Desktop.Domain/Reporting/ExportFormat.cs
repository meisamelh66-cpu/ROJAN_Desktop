namespace Rojan.Desktop.Domain.Reporting;

/// <summary>Export targets the Export architecture supports. Phase 20 ships the architecture end-to-end and a genuine CSV implementation; Pdf/Excel/Print are honest stubs (see <c>Application.Reporting.IReportExportService</c>).</summary>
public enum ExportFormat
{
    Pdf,
    Excel,
    Csv,
    Print,
}
