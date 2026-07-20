using Rojan.Desktop.Domain.Notifications;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>In-memory <see cref="INotificationRepository"/> test double - avoids touching the real %LocalAppData% JSON file <c>LocalNotificationRepository</c> persists to.</summary>
internal sealed class StubNotificationRepository : INotificationRepository
{
    private readonly List<AppNotification> _notifications = [];

    public Task<IReadOnlyList<AppNotification>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AppNotification>>(_notifications);

    public Task AddAsync(AppNotification notification, CancellationToken cancellationToken = default)
    {
        _notifications.Insert(0, notification);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AppNotification notification, CancellationToken cancellationToken = default)
    {
        var index = _notifications.FindIndex(n => n.Id == notification.Id);
        if (index >= 0)
        {
            _notifications[index] = notification;
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        _notifications.RemoveAll(n => n.Id == notificationId);
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _notifications.Clear();
        return Task.CompletedTask;
    }
}
