using System.Globalization;

namespace Rojan.Desktop.Application.AI;

public sealed class NotificationInsightService : INotificationInsightService
{
    private readonly IInsightEngine _insightEngine;

    public NotificationInsightService(IInsightEngine insightEngine)
    {
        _insightEngine = insightEngine;
    }

    public async Task<IReadOnlyList<SmartNotificationDto>> GetSmartNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var insights = await _insightEngine.GenerateInsightsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return insights
            .Where(i => i.Severity is InsightSeverity.Risk or InsightSeverity.Critical or InsightSeverity.Opportunity)
            .Select(BuildNotification)
            .ToList();
    }

    private static SmartNotificationDto BuildNotification(AIInsightDto insight)
    {
        var message = string.Create(CultureInfo.InvariantCulture, $"{insight.Title} - {insight.Description}");
        return new SmartNotificationDto(
            string.Create(CultureInfo.InvariantCulture, $"notif-{insight.Id}"),
            insight.Severity,
            message,
            DateTimeOffset.Now);
    }
}
