namespace Rojan.Desktop.Application.AI;

/// <summary>Computes the AI Dashboard's headline Business Health Score - a weighted composite over live KPI data, via <c>Domain.AI.BusinessHealthCalculator</c>.</summary>
public interface IBusinessHealthService
{
    public Task<BusinessHealthScoreDto> ComputeScoreAsync(CancellationToken cancellationToken = default);
}
