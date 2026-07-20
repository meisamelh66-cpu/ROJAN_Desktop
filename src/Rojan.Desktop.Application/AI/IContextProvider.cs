namespace Rojan.Desktop.Application.AI;

/// <summary>
/// The Prompt System's Business Context block - a plain-text snapshot of
/// overall business state, composed from
/// <c>Reporting.IAnalyticsQueryService</c> (already-published, reused
/// unchanged - see this phase's "reuse all existing services, no tight
/// coupling" instruction) rather than reaching into any other module's
/// repository directly.
/// </summary>
public interface IContextProvider
{
    public Task<string> GetBusinessContextAsync(CancellationToken cancellationToken = default);
}
