using Rojan.Desktop.Application.Notifications;

namespace Rojan.Desktop.Presentation.Notifications;

/// <summary>
/// Phase 27: Enterprise Notification Center. A notification with every
/// field already resolved to display text - the shape
/// <c>ViewModels.Notifications.NotificationCenterViewModel</c> and the
/// toast surface actually bind to.
/// <see cref="INotificationContentResolver"/> is the only place a
/// <c>Application.Notifications.NotificationDto</c> is turned into one
/// of these, since only Presentation can see <c>Strings</c> - mirrors
/// Phase 26's <c>Help.ResolvedHelpContent</c>.
/// </summary>
public sealed record ResolvedNotification(
    string Id,
    NotificationSeverity Severity,
    NotificationPriority Priority,
    string Title,
    string Message,
    string CategoryLabel,
    string GroupLabel,
    DateTimeOffset CreatedAt,
    bool IsRead);
