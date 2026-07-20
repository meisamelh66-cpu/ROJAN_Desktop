namespace Rojan.Desktop.Application.Notifications;

/// <summary>
/// Phase 27: Enterprise Notification Center. The one centralized entry
/// point every module raises a user-facing notification through - no
/// module talks to <c>Domain.Notifications.INotificationRepository</c>
/// directly. Owns the notification history, read/unread state, Silent
/// Mode preference, and the badge counter's source of truth
/// (<see cref="GetUnreadCountAsync"/>).
/// </summary>
public interface INotificationService
{
    /// <summary>Fires for every notification once persisted - the signal <c>NotificationCenterViewModel</c> refreshes its list from.</summary>
    public event EventHandler<NotificationDto>? NotificationRaised;

    /// <summary>
    /// Fires only for the subset of raised notifications that should
    /// actually produce a toast popup, per <c>Domain.Notifications.NotificationRules.ShouldShowToast</c>
    /// (already Silent-Mode-filtered) - the signal
    /// <c>ToastHostViewModel</c> subscribes to. Deliberately separate
    /// from <see cref="NotificationRaised"/> so no Presentation consumer
    /// has to re-derive the Silent Mode rule itself.
    /// </summary>
    public event EventHandler<NotificationDto>? ToastRequested;

    /// <summary>Fires after any mutation (mark-read, mark-all-read, dismiss, clear, Silent Mode toggle) - the signal the badge counter and Notification Center list both refresh from, the same <c>StateChanged</c> naming convention <c>ICurrentSessionService</c> already established.</summary>
    public event EventHandler? StateChanged;

    public Task<NotificationDto> RaiseAsync(NotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Every persisted notification, most-recent-first within priority-descending order (see <c>Domain.Notifications.NotificationRules.PriorityRank</c>).</summary>
    public Task<IReadOnlyList<NotificationDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>The Badge Counter requirement's source of truth - how many persisted notifications are currently unread.</summary>
    public Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    public Task MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default);

    public Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);

    /// <summary>Removes one notification from history - the "dismiss" action.</summary>
    public Task DismissAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>Removes every notification from history - the "clear all" action.</summary>
    public Task ClearAllAsync(CancellationToken cancellationToken = default);

    public Task<bool> GetIsSilentModeEnabledAsync(CancellationToken cancellationToken = default);

    public Task SetSilentModeEnabledAsync(bool isEnabled, CancellationToken cancellationToken = default);
}
