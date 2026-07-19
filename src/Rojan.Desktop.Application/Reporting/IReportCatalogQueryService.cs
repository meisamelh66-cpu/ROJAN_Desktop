namespace Rojan.Desktop.Application.Reporting;

/// <summary>The report catalog the Report Viewer browses/searches - a thin read-through over <see cref="Rojan.Desktop.Domain.Reporting.IReportingRepository"/>'s system-defined report definitions.</summary>
public interface IReportCatalogQueryService
{
    public Task<IReadOnlyList<ReportDefinitionDto>> GetReportDefinitionsAsync(CancellationToken cancellationToken = default);

    public Task<ReportDefinitionDto?> GetReportDefinitionByIdAsync(string reportDefinitionId, CancellationToken cancellationToken = default);
}
