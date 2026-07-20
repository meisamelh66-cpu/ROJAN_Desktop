namespace Rojan.Desktop.Application.AI;

/// <summary>Filters <see cref="IInsightEngine"/>'s output down to <see cref="SmartNotificationDto"/>s - single-sentence, immediately-actionable alerts for the Smart Notifications feature.</summary>
public interface INotificationInsightService
{
    public Task<IReadOnlyList<SmartNotificationDto>> GetSmartNotificationsAsync(CancellationToken cancellationToken = default);
}
