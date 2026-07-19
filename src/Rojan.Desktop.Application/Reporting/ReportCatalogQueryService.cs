using DomainReporting = Rojan.Desktop.Domain.Reporting;

namespace Rojan.Desktop.Application.Reporting;

public sealed class ReportCatalogQueryService : IReportCatalogQueryService
{
    private readonly DomainReporting.IReportingRepository _repository;

    public ReportCatalogQueryService(DomainReporting.IReportingRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ReportDefinitionDto>> GetReportDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var definitions = await _repository.GetReportDefinitionsAsync(cancellationToken).ConfigureAwait(false);
        return definitions.Select(ReportingMapper.MapDefinition).ToList();
    }

    public async Task<ReportDefinitionDto?> GetReportDefinitionByIdAsync(string reportDefinitionId, CancellationToken cancellationToken = default)
    {
        var definition = await _repository.GetReportDefinitionByIdAsync(reportDefinitionId, cancellationToken).ConfigureAwait(false);
        return definition is null ? null : ReportingMapper.MapDefinition(definition);
    }
}
