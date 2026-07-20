using Rojan.Desktop.Application.AI;

namespace Rojan.Desktop.Application.Tests.AI;

public sealed class RecommendationEngineTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Now;

    private static AIInsightDto BuildInsight(string id, InsightSeverity severity, InsightCategory category = InsightCategory.Revenue) =>
        new(id, category, severity, $"Title {id}", $"Description {id}", 100m, 12m, Now);

    [Fact]
    public async Task GenerateRecommendationsAsync_OnlyIncludesRiskOpportunityAndCriticalInsights()
    {
        IReadOnlyList<AIInsightDto> insights =
        [
            BuildInsight("i-info", InsightSeverity.Info),
            BuildInsight("i-trend", InsightSeverity.Trend),
            BuildInsight("i-risk", InsightSeverity.Risk),
            BuildInsight("i-opportunity", InsightSeverity.Opportunity),
            BuildInsight("i-critical", InsightSeverity.Critical),
        ];
        var sut = new RecommendationEngine(new StubInsightEngine(insights));

        var recommendations = await sut.GenerateRecommendationsAsync();

        Assert.Equal(3, recommendations.Count);
        Assert.DoesNotContain(recommendations, r => r.Id == "rec-i-info");
        Assert.DoesNotContain(recommendations, r => r.Id == "rec-i-trend");
    }

    [Theory]
    [InlineData(InsightSeverity.Critical, RecommendationPriority.Urgent)]
    [InlineData(InsightSeverity.Risk, RecommendationPriority.High)]
    [InlineData(InsightSeverity.Opportunity, RecommendationPriority.Medium)]
    public async Task GenerateRecommendationsAsync_MapsSeverityToPriority(InsightSeverity severity, RecommendationPriority expectedPriority)
    {
        var sut = new RecommendationEngine(new StubInsightEngine([BuildInsight("i1", severity)]));

        var recommendations = await sut.GenerateRecommendationsAsync();

        Assert.Equal(expectedPriority, recommendations.Single().Priority);
    }

    [Fact]
    public async Task GenerateSuggestedTasksAsync_OnlyIncludesHighAndUrgentPriorityRecommendations()
    {
        IReadOnlyList<AIInsightDto> insights =
        [
            BuildInsight("i-critical", InsightSeverity.Critical),
            BuildInsight("i-risk", InsightSeverity.Risk),
            BuildInsight("i-opportunity", InsightSeverity.Opportunity),
        ];
        var sut = new RecommendationEngine(new StubInsightEngine(insights));

        var tasks = await sut.GenerateSuggestedTasksAsync();

        Assert.Equal(2, tasks.Count);
        Assert.DoesNotContain(tasks, t => t.Priority == RecommendationPriority.Medium);
    }

    [Fact]
    public async Task GenerateSuggestedTasksAsync_UrgentTasksAreDueSoonerThanHighPriorityTasks()
    {
        IReadOnlyList<AIInsightDto> insights =
        [
            BuildInsight("i-critical", InsightSeverity.Critical),
            BuildInsight("i-risk", InsightSeverity.Risk),
        ];
        var sut = new RecommendationEngine(new StubInsightEngine(insights));

        var tasks = await sut.GenerateSuggestedTasksAsync();

        var urgentTask = tasks.Single(t => t.Priority == RecommendationPriority.Urgent);
        var highTask = tasks.Single(t => t.Priority == RecommendationPriority.High);
        Assert.True(urgentTask.SuggestedDueDate < highTask.SuggestedDueDate);
    }
}
