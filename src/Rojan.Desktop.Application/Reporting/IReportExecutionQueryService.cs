namespace Rojan.Desktop.Application.Reporting;

/// <summary>
/// The Reporting platform's aggregation engine - runs one
/// <see cref="ReportDefinitionDto"/> against the given filters and
/// produces a <see cref="ReportResultDto"/>, composing every source
/// module's own Application-layer query services (never their
/// repositories/Domain types directly). Every method is cancellable per
/// this phase's Performance requirement - report generation must not
/// block the UI thread and must support cancellation.
/// </summary>
public interface IReportExecutionQueryService
{
    public Task<ReportResultDto> RunReportAsync(string reportDefinitionId, IReadOnlyList<ReportFilterDto> filters, CancellationToken cancellationToken = default);
}
