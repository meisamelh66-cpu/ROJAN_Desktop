namespace Rojan.Desktop.Application.AI;

/// <summary>Derives <see cref="AIRecommendationDto"/>s and <see cref="SuggestedTaskDto"/>s from <see cref="IInsightEngine"/>'s output - the Recommendations Panel and Action Center's content, computed fresh on every call, never persisted.</summary>
public interface IRecommendationEngine
{
    public Task<IReadOnlyList<AIRecommendationDto>> GenerateRecommendationsAsync(CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SuggestedTaskDto>> GenerateSuggestedTasksAsync(CancellationToken cancellationToken = default);
}
