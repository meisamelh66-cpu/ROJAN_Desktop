namespace Rojan.Desktop.Application.AI;

/// <summary>Produces the Daily Summary and Executive Summary features - a narrative built from <see cref="IContextProvider"/>'s business snapshot plus <see cref="IInsightEngine"/>'s highest-severity findings, computed fresh on every call.</summary>
public interface ISummaryEngine
{
    public Task<BusinessSummaryDto> GetDailySummaryAsync(CancellationToken cancellationToken = default);

    public Task<BusinessSummaryDto> GetExecutiveSummaryAsync(CancellationToken cancellationToken = default);
}
