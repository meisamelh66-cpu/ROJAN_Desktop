using Rojan.Desktop.Application.AI;

namespace Rojan.Desktop.Application.Tests.AI;

public sealed class NotificationInsightServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Now;

    private static AIInsightDto BuildInsight(string id, InsightSeverity severity) =>
        new(id, InsightCategory.Revenue, severity, $"Title {id}", $"Description {id}", 100m, 12m, Now);

    [Fact]
    public async Task GetSmartNotificationsAsync_OnlyIncludesRiskCriticalAndOpportunityInsights()
    {
        IReadOnlyList<AIInsightDto> insights =
        [
            BuildInsight("i-info", InsightSeverity.Info),
            BuildInsight("i-trend", InsightSeverity.Trend),
            BuildInsight("i-risk", InsightSeverity.Risk),
            BuildInsight("i-critical", InsightSeverity.Critical),
            BuildInsight("i-opportunity", InsightSeverity.Opportunity),
        ];
        var sut = new NotificationInsightService(new StubInsightEngine(insights));

        var notifications = await sut.GetSmartNotificationsAsync();

        Assert.Equal(3, notifications.Count);
        Assert.DoesNotContain(notifications, n => n.Id == "notif-i-info");
        Assert.DoesNotContain(notifications, n => n.Id == "notif-i-trend");
    }

    [Fact]
    public async Task GetSmartNotificationsAsync_MessageIncludesTitleAndDescription()
    {
        var sut = new NotificationInsightService(new StubInsightEngine([BuildInsight("i-risk", InsightSeverity.Risk)]));

        var notifications = await sut.GetSmartNotificationsAsync();

        var notification = notifications.Single();
        Assert.Contains("Title i-risk", notification.Message, StringComparison.Ordinal);
        Assert.Contains("Description i-risk", notification.Message, StringComparison.Ordinal);
        Assert.Equal(InsightSeverity.Risk, notification.Severity);
    }
}
