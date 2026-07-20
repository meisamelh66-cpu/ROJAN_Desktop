using Rojan.Desktop.Application.Notifications;

namespace Rojan.Desktop.Presentation.Tests.Notifications;

/// <summary>In-memory <see cref="INotificationService"/> test double - lets a ViewModel test control exactly which notifications exist and fire events on demand, without a real repository/store.</summary>
internal sealed class StubNotificationService : INotificationService
{
    private readonly List<NotificationDto> _notifications = [];
    private bool _isSilentModeEnabled;

    public event EventHandler<NotificationDto>? NotificationRaised;

    public event EventHandler<NotificationDto>? ToastRequested;

    public event EventHandler? StateChanged;

    public void Seed(IEnumerable<NotificationDto> notifications) => _notifications.AddRange(notifications);

    public void RaiseToastRequested(NotificationDto notification) => ToastRequested?.Invoke(this, notification);

    public Task<NotificationDto> RaiseAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        var dto = new NotificationDto(
            Guid.NewGuid().ToString("N"),
            request.Severity,
            request.Priority,
            request.TitleKey,
            request.MessageKey,
            request.MessageArgs ?? [],
            request.Category,
            request.GroupKey ?? request.Category,
            DateTimeOffset.UtcNow,
            false);
        _notifications.Insert(0, dto);
        NotificationRaised?.Invoke(this, dto);
        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.FromResult(dto);
    }

    public Task<IReadOnlyList<NotificationDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NotificationDto>>(_notifications);

    public Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_notifications.Count(n => !n.IsRead));

    public Task MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        var index = _notifications.FindIndex(n => n.Id == notificationId);
        if (index >= 0)
        {
            _notifications[index] = _notifications[index] with { IsRead = true };
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < _notifications.Count; i++)
        {
            _notifications[i] = _notifications[i] with { IsRead = true };
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task DismissAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        _notifications.RemoveAll(n => n.Id == notificationId);
        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        _notifications.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task<bool> GetIsSilentModeEnabledAsync(CancellationToken cancellationToken = default) => Task.FromResult(_isSilentModeEnabled);

    public Task SetSilentModeEnabledAsync(bool isEnabled, CancellationToken cancellationToken = default)
    {
        _isSilentModeEnabled = isEnabled;
        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}
