namespace Rojan.Desktop.Domain.Notifications;

/// <summary>Phase 27: Enterprise Notification Center. Persisted notification history - implemented by <c>Infrastructure.Notifications.LocalNotificationRepository</c>, same repository-pattern shape every other module uses.</summary>
public interface INotificationRepository
{
    public Task<IReadOnlyList<AppNotification>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task AddAsync(AppNotification notification, CancellationToken cancellationToken = default);

    /// <summary>Persists <paramref name="notification"/> in place of whichever existing entry shares its <see cref="AppNotification.Id"/> - the mechanism <c>NotificationService</c> uses for mark-read/mark-unread.</summary>
    public Task UpdateAsync(AppNotification notification, CancellationToken cancellationToken = default);

    /// <summary>Removes one notification - the "dismiss" action.</summary>
    public Task RemoveAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>Removes every notification - the "clear all" action.</summary>
    public Task ClearAsync(CancellationToken cancellationToken = default);
}
